﻿using Health.Core.Entities;

namespace Health.Core.API.Repository
{
    /// <summary>
    /// Репозиторий доступа к актуальным данным сессии.
    /// </summary>
    public interface IActualCredentialRepository
    {
        /// <summary>
        /// Запись данных.
        /// </summary>
        /// <param name="identifier">Идентификатор.</param>
        /// <param name="credential">Мандат пользователя.</param>
        void Write(string identifier, UserCredential credential);

        /// <summary>
        /// Чтение данных.
        /// </summary>
        /// <param name="identifier">Идентификатор.</param>
        /// <returns>Мандат пользователя.</returns>
        UserCredential Read(string identifier);

        /// <summary>
        /// Очистка.
        /// </summary>
        void Clear();
    }
}