﻿namespace Algorithms.LinearProgramming
{
    /// <summary>Результат работы симплекс-метода</summary>
    public enum SimplexResult
    {
        /// <summary>Успех</summary>
        Success = 0,
        /// <summary>Множество допустимых x пусто</summary>
        HullIsEmpty,
        /// <summary>Цикл из-за округления</summary>
        CycleDetected,
        /// <summary>Функционал неограничен сверху</summary>
        FunctionalUnbound,
        /// <summary>Не удалось посчитать стартовую точку из-за ошибок округления</summary>
        RoundingError,
        /// <summary>Неизвестная ошибка</summary>
        UnknownError
    }
}