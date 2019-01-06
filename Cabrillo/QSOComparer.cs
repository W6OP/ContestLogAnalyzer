using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace W6OP.ContestLogAnalyzer
{
    public class QSOComparer : IEquatable<QSO>
    {
        public bool Equals(QSO other)
        {
            if (Object.ReferenceEquals(other, null)) return false;


            // Check whether the compared object references the same data.

            if (Object.ReferenceEquals(this, other)) return true;

            return other.ContactEntity.Equals(other.ContactEntity);
        }

        public int GetHashCode(QSO qso)

        {

            //Check whether the object is null
            if (Object.ReferenceEquals(qso, null)) return 0;

            //Get hash code for the Name field if it is not null.
            int hashProductName = qso.ContactEntity == null ? 0 : qso.ContactEntity.GetHashCode();

            //Get hash code for the Code field.
            int hashProductCode = qso.ContactCall.GetHashCode();

            //Calculate the hash code for the product.
            return hashProductName ^ hashProductCode;
        }

    }
}
