﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Health.Site.Models.Rules
{
    [Serializable]
    public class RangeValidatorConfig : IValidatorRuleConfig
    {
        public double Min { get; set; }

        public double Max { get; set; }

        #region Implementation of IValidatorRuleConfig

        public string ErrorMessage { get; set; }

        #endregion
    }

    public class RangeValidatorRule : ModelValidatorRule, IModelValidatorRule
    {
        #region Implementation of IModelValidatorRule

        public ModelValidator Create(IValidatorRuleConfig rule_config, ModelMetadata model_metadata, ControllerContext controller_context)
        {
            if (rule_config is RangeValidatorConfig)
            {
                var config = rule_config as RangeValidatorConfig;
                var attribute = new RangeAttribute(config.Min, config.Max)
                                    {
                                        ErrorMessage = config.ErrorMessage
                                    };
                var adapter = new RangeAttributeAdapter(model_metadata, controller_context, attribute);
                return adapter;
            }
            throw GenerateConfigException(GetType(), typeof (RangeValidatorConfig), rule_config.GetType());
        }

        #endregion
    }
}