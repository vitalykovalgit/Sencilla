using Sencilla.Mobile.Core.Attribute;

namespace Sencilla.Mobile.Core.Filter
{
    public class Filter : IEntity
    {
        /// <summary>
        /// Default constructor for serialization
        /// </summary>
        public Filter()
        {
        }

        public Filter(int? skip, int? take) : this()
        {
            Skip = skip;
            Take = take;

            OrderDirect = true;
        }

        /// <summary>
        /// Implements IEntity interface 
        /// </summary>
        [ApiSkipInUrl]
        public int Id { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int? Skip { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int? Take { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool? OrderDirect { get; set; } 

        /// <summary>
        /// 
        /// </summary>
        public string OrderColumn { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string[] Include { get; set; }
    }
}
