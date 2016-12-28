using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Systek.Net
{
    /// <summary>
    /// Contains data representing the most recent state of the server
    /// </summary>
    [Serializable]
    public class UpdateData
    {
        /// <summary>
        /// Gets or sets the name of the host.
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// Gets or sets the authentication key.
        /// </summary>
        public string AuthKey { get; set; }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object other)
        {
            // Type check
            if ((other == null) || (other.GetType() != typeof(UpdateData)))
            {
                return false;
            }

            UpdateData test = (UpdateData)other;

            // Comparison of primitives
            if ((HostName != test.HostName) || (AuthKey != test.AuthKey))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
