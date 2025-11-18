  namespace TheFoundersPleas.Core.Enums
{
    /// <summary>
    /// Тип объекта, размещаемого на клетке.
    /// </summary>
    public enum FeatureType
    {
        /// <summary>
        /// Объект отсутствует.
        /// </summary>
        None = 0,

        /// <summary>
        /// Животное.
        /// </summary>
        Animal = 1,

        /// <summary>
        /// Растение.
        /// </summary>
        Plant = 2,

        /// <summary>
        /// Ископаемый ресурс.
        /// </summary>
        Mineral = 3,

        /// <summary>
        /// Структура / постройка.
        /// </summary>
        Structure = 4,
    }
}