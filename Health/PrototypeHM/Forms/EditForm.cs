﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EFCFModel;
using EFCFModel.Attributes;
using PrototypeHM.Components;
using PrototypeHM.DI;
using ByteConverter = EFCFModel.ByteConverter;

namespace PrototypeHM.Forms
{
    public partial class EditForm : DIForm
    {
        private readonly DbContext _dbContext;
        private readonly Type _etype;
        private readonly IList<Control> _form;
        private readonly IList<string> _labels;
        private readonly ISchemaManager _schemaManager;
        private readonly IValidator _validator;
        private readonly CancellationTokenSource _loadCancellationTokenSource;
        private readonly SynchronizationContext _synchronizationContext;
        private Task<object> _loadTask;
        private object _data;
        private int _key;
        private int _top;

        public EditForm(IDIKernel diKernel, Type etype, int key) : base(diKernel)
        {
            UID = etype.FullName + key;
            _synchronizationContext = SynchronizationContext.Current;
            _loadCancellationTokenSource = new CancellationTokenSource();
            InitializeComponent();
            Text = string.Format("Редактирование {0} #{1}", etype.GetDisplayName(), key);
            _etype = etype;
            _key = key;
            _dbContext = Get<DbContext>();
            _schemaManager = Get<ISchemaManager>();
            _validator = Get<IValidator>();
            _form = new List<Control>();
            _labels = new List<string>();
            loadControl.Show();
            Load += EditFormLoad;
        }

        private void EditFormLoad(object sender, EventArgs e)
        {
            InitializeData();
        }

        private void InitializeData()
        {
            if (_schemaManager.HasKey(_etype))
            {
                _loadTask = new Task<object>(() =>
                                             _data = _key != -1
                                                         ? ((IQueryable<object>) _dbContext.Set(_etype)).FirstOrDefault(ExQueryable.PropertyFilter(_etype, _schemaManager.Key(_etype).Name, _key))
                                                         : _dbContext.Set(_etype).Create()
                                             , _loadCancellationTokenSource.Token);
                _loadTask.ContinueWith(task =>
                                           {
                                               InitializeProperties();
                                               InitializeRelation();
                                               _synchronizationContext.Post(c =>
                                                                                {
                                                                                    InitializeControls();
                                                                                    loadControl.Hide();
                                                                                }, null);
                                           }, TaskContinuationOptions.OnlyOnRanToCompletion);
                _loadTask.Start();
            }
        }

        private string ComponentNameForProperty(string name)
        {
            return string.Format("componentFor{0}", name);
        }

        private Control ControlForProperty(string name)
        {
            return layoutPanel.Controls[ComponentNameForProperty(name)];
        }

        private void ValidateProperty(PropertyDescriptor descriptor)
        {
            ValidationResult result = _validator.ValidateProperty(_data, descriptor);
            if (result != null) errorProvider.SetError(ControlForProperty(result.Descriptor.Name), result.ErrorMessage);
        }

        private bool ValidateObject()
        {
            bool isValid = _validator.IsValid(_data);
            if (!isValid)
            {
                foreach (ValidationResult error in _validator.Errors) errorProvider.SetError(ControlForProperty(error.Descriptor.Name), error.ErrorMessage);
                statusPanel.Text = @"Исправте ошибки.";
            }
            return isValid;
        }

        private void InitializeProperties()
        {
            //TODO: с этим надо что-то делать.
            _top = 0;
            IEnumerable<PropertyDescriptor> properties = TypeDescriptor.GetProperties(_etype).Cast<PropertyDescriptor>().
                Where(p => !p.Attributes.Cast<Attribute>().Any(a => a is NotEditAttribute));
            foreach (PropertyDescriptor property in properties)
            {
                PropertyDescriptor prop = property;
                object propertyValue = prop.GetValue(_data);
                if (prop.Name ==
                    _schemaManager.Key(_etype).Name) continue;
                Type propertyType = prop.PropertyType;
                Control control = null;
                if (propertyType == typeof (int))
                {
                    var c = new NumericUpDown {Name = ComponentNameForProperty(prop.Name)};
                    c.DataBindings.Add("Value", _data, prop.Name, false, DataSourceUpdateMode.OnPropertyChanged);
                    c.ValueChanged += (sender, e) => ValidateProperty(prop);
                    control = c;
                }
                if (propertyType == typeof (double))
                {
                    var c = new NumericUpDown {
                                                  Name = ComponentNameForProperty(prop.Name),
                                                  DecimalPlaces = 2,
                                                  Increment = (decimal) 0.5
                                              };
                    c.DataBindings.Add("Value", _data, prop.Name, false, DataSourceUpdateMode.OnPropertyChanged);
                    c.ValueChanged += (sender, e) => ValidateProperty(prop);
                    control = c;
                }
                if (propertyType == typeof (string))
                {
                    var c = new TextBox {
                                            Name = ComponentNameForProperty(prop.Name),
                                            Text = prop.GetValue(_data) as string
                                        };
                    EditModeAttribute att = prop.Attributes.OfType<EditModeAttribute>().FirstOrDefault();
                    if (att != null &&
                        att.GetEditMode() == EditMode.Multiline)
                    {
                        c.Multiline = true;
                        c.Height = c.Height*4;
                        c.Width = c.Width*3;
                        c.ScrollBars = ScrollBars.Vertical;
                    }
                    c.TextChanged += (sender, e) => prop.SetValue(_data, c.Text);
                    c.TextChanged += (sender, e) => ValidateProperty(prop);
                    control = c;
                }
                if (propertyType == typeof (DateTime))
                {
                    DateTime value = propertyValue == null || (DateTime) propertyValue == default(DateTime)
                                         ? DateTime.Now
                                         : (DateTime) propertyValue;
                    var c = new DateTimePicker {Name = ComponentNameForProperty(prop.Name), Value = value};
                    c.ValueChanged += (sender, e) => prop.SetValue(_data, c.Value);
                    c.ValueChanged += (sender, e) => ValidateProperty(prop);
                    prop.SetValue(_data, value);
                    control = c;
                }
                if (propertyType == typeof (byte[]))
                {
                    ByteTypeAttribute byteTypeAttribute = prop.Attributes.OfType<ByteTypeAttribute>().FirstOrDefault();
                    if (byteTypeAttribute != null)
                    {
                        var propertyByteValue = (byte[]) propertyValue;
                        Type byteType = byteTypeAttribute.GetByteType(_data);
                        if (byteType == typeof (bool))
                        {
                            bool value = propertyValue != null
                                             ? Get<ByteConverter>().To<bool>(propertyByteValue)
                                             : default(bool);
                            var c = new CheckBox {Name = ComponentNameForProperty(prop.Name), Checked = value};
                            c.CheckedChanged += (sender, e) => prop.SetValue(_data, Get<ByteConverter>().Get(c.Checked));
                            c.CheckedChanged += (sender, e) => ValidateProperty(prop);
                            control = c;
                        }
                        if (byteType == typeof (int))
                        {
                            var c = new NumericUpDown {Name = ComponentNameForProperty(prop.Name)};
                            decimal value = propertyValue != null
                                                ? Get<ByteConverter>().To<int>(propertyByteValue)
                                                : c.Minimum;
                            c.Value = value;
                            c.ValueChanged +=
                                (sender, e) => prop.SetValue(_data, Get<ByteConverter>().Get(Convert.ToInt32(c.Value)));
                            c.ValueChanged += (sender, e) => ValidateProperty(prop);
                            control = c;
                        }
                        if (byteType == typeof (double))
                        {
                            var c = new NumericUpDown {
                                                          Name = ComponentNameForProperty(prop.Name),
                                                          DecimalPlaces = 2,
                                                          Increment = (decimal) 0.5
                                                      };
                            decimal value = propertyValue != null
                                                ? (decimal) Get<ByteConverter>().To<double>(propertyByteValue)
                                                : c.Minimum;
                            c.Value = value;
                            c.ValueChanged +=
                                (sender, e) => prop.SetValue(_data, Get<ByteConverter>().Get(Convert.ToDouble(c.Value)));
                            c.ValueChanged += (sender, e) => ValidateProperty(prop);
                            control = c;
                        }
                        if (byteType == typeof (string))
                        {
                            string text = propertyValue == null
                                              ? string.Empty
                                              : Get<ByteConverter>().To<string>(propertyByteValue);
                            var c = new TextBox {
                                                    Name = ComponentNameForProperty(prop.Name),
                                                    Text = text
                                                };
                            c.TextChanged += (sender, e) => prop.SetValue(_data, Get<ByteConverter>().Get(c.Text));
                            c.TextChanged += (sender, e) => ValidateProperty(prop);
                            control = c;
                        }
                        if (byteType == typeof (DateTime))
                        {
                            DateTime value = propertyValue != null
                                                 ? Get<ByteConverter>().To<DateTime>(propertyByteValue)
                                                 : DateTime.Now;
                            var c = new DateTimePicker {Name = ComponentNameForProperty(prop.Name), Value = value};
                            c.ValueChanged += (sender, e) => prop.SetValue(_data, Get<ByteConverter>().Get(c.Value));
                            c.ValueChanged += (sender, e) => ValidateProperty(prop);
                            control = c;
                        }
                    }
                }
                if (control != null)
                {
                    control.Leave += (sender, e) => ValidateProperty(prop);
                    control.Top = _top;
                    _top += control.Height;
                    _form.Add(control);
                    DisplayNameAttribute att = prop.Attributes.OfType<DisplayNameAttribute>().FirstOrDefault();
                    _labels.Add(att == null ? prop.Name : att.DisplayName);
                }
            }
        }

        private void InitializeRelation()
        {
            IList<Relation> relations = _schemaManager.GetRelations(_etype);
            foreach (Relation relation in relations)
            {
                Control control = null;
                Relation rel = relation;
                if (rel.RelationType == RelationType.ManyToOne)
                {
                    var c = new SingleSelector(DIKernel, rel.FromProperty.PropertyType) {
                                                                                            SelectedData = rel.FromProperty.GetValue(_data, null),
                                                                                            ValueChange = o => rel.FromProperty.SetValue(_data, o, null),
                                                                                            Dock = DockStyle.Fill
                                                                                        };
                    control = c;
                }
                if (rel.RelationType == RelationType.ManyToMany)
                {
                    object left = rel.FromProperty.GetValue(_data, null);
                    var c = new MultiSelector(DIKernel) {
                                                            LeftLoad = () => new BindingSource {DataSource = left},
                                                            RightLoad = () => new BindingSource {DataSource = ((IEnumerable<object>) _dbContext.Set(rel.ToType)).Where(e => !((IEnumerable<object>) left).Contains(e)).IterateToList(rel.ToType)},
                                                            Dock = DockStyle.Fill
                                                        };
                    c.LoadData(LoadMode.Async);
                    control = c;
                }
                if (control != null)
                {
                    control.Name = ComponentNameForProperty(relation.FromProperty.Name);
                    _form.Add(control);
                    var att =
                        rel.FromProperty.GetCustomAttributes(true).FirstOrDefault(a => a is DisplayNameAttribute)
                        as DisplayNameAttribute;
                    _labels.Add(att == null ? rel.FromProperty.Name : att.DisplayName);
                }
            }
        }

        private void InitializeControls()
        {
            layoutPanel.ColumnCount = 2;
            layoutPanel.RowCount = _form.Count;
            for (int r = 0; r < layoutPanel.RowCount; r++)
            {
                for (int c = 0; c < layoutPanel.ColumnCount; c += 2)
                {
                    var label = new Label
                                    {
                                        Text = _labels[r],
                                        Top = 5,
                                        Dock = DockStyle.Fill,
                                        TextAlign = ContentAlignment.TopLeft,
                                        Margin = new Padding(0, 5, 0, 5)
                                    };
                    _form[r].Margin = new Padding(0, 5, 0, 5);
                    layoutPanel.Controls.Add(label, c, r);
                    layoutPanel.Controls.Add(_form[r], c + 1, r);    
                }
            }
            var saveButton = new ToolStripButton("Сохранить");
            saveButton.Click += SaveButtonClick;
            toolPanel.Items.Add(saveButton);
        }

        private void SaveButtonClick(object sender, EventArgs e)
        {
            try
            {
                if (ValidateObject())
                {
                    if (_key == -1) _dbContext.Set(_etype).Add(_data);
                    _dbContext.SaveChanges();
                    if (_key == -1)
                    {
                        _key = Convert.ToInt32(_schemaManager.Key(_etype).GetValue(_data, null));
                        UID = _etype.FullName + _key;
                    }
                    statusPanel.Text = string.Format("Сохранено {0}.", DateTime.Now);
                }
            }
            catch (Exception exp)
            {
                YMessageBox.Error(exp.Message);
            }
        }

        private void EditFormClosing(object sender, FormClosingEventArgs e)
        {
            if (_loadTask.Status == TaskStatus.Running &&
                _loadCancellationTokenSource.Token.CanBeCanceled) _loadCancellationTokenSource.Cancel();
        }
    }
}