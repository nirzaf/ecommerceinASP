namespace Nop.Core.Domain.Customers
{
    /// <summary>
    /// Represents a customer-customer role mapping class
    /// </summary>
    public partial class CustomerCustomerRoleMapping : BaseEntity
    {
        /// <summary>
        /// Gets or sets the customer identifier
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Gets or sets the customer role identifier
        /// </summary>
        public int CustomerRoleId { get; set; }

        /// <summary>
        /// Gets or sets the customer
        /// </summary>
        public virtual Customer Customer { get; set; }

        /// <summary>
        /// Gets or sets the customer role
        /// </summary>
        public virtual CustomerRole CustomerRole { get; set; }
    }
}