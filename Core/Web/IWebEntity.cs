
namespace Sencilla.Core.Web
{
    /// <summary>
    /// Generic class for all the DTO/WebEntity classes 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public interface IWebEntity<TEntity, TKey>
    {
        /// <summary>
        /// Id 
        /// </summary>
        TKey Id { get; set; }

        /// <summary>
        /// Converter 
        /// </summary>
        /// <returns></returns>
        TEntity ToEntity(TEntity entity);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        void FromEntity(TEntity entity);
    }


}
