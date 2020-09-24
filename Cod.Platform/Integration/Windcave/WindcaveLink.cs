using System;
using System.Collections.Generic;

namespace Cod.Platform
{
    internal struct WindcaveLink : IEquatable<WindcaveLink>
    {
        public string HREF { get; set; }

        public string Rel { get; set; }

        public string Method { get; set; }

        public override bool Equals(object obj) => obj is WindcaveLink link && this.Equals(link);

        public bool Equals(WindcaveLink other) => this.HREF == other.HREF && this.Rel == other.Rel && this.Method == other.Method;

        public override int GetHashCode()
        {
            var hashCode = 1653203420;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.HREF);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.Rel);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.Method);
            return hashCode;
        }

        public static bool operator ==(WindcaveLink left, WindcaveLink right) => left.Equals(right);

        public static bool operator !=(WindcaveLink left, WindcaveLink right) => !(left == right);
    }
}
