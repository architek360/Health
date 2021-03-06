﻿namespace Health.Core.API.Validators
{
    /// <summary>
    /// Фабрика валидаторов.
    /// </summary>
    public interface IValidatorFactory
    {
        /// <summary>
        /// Сообщение с информацией почему не прошла проверка.
        /// </summary>
        string Message { get; }

        /// <summary>
        /// Проверяет значение на валидность согласно типу валидатора.
        /// </summary>
        /// <param name="validator_type">Тип валидатора.</param>
        /// <param name="value">Значение.</param>
        /// <returns>Результата проверки.</returns>
        bool IsValid(string validator_type, object value);
    }
}