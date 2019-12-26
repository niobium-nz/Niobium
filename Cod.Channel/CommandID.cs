using System;
using System.Collections.Generic;

namespace Cod.Channel
{
    public struct CommandID : IEquatable<CommandID>
    {
        public string Group { get; set; }

        public string Name { get; set; }

        public override bool Equals(object obj) => obj is CommandID iD && this.Equals(iD);
        public bool Equals(CommandID other) => this.Group == other.Group && this.Name == other.Name;

        public override int GetHashCode()
        {
            var hashCode = -570022382;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.Group);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.Name);
            return hashCode;
        }

        public static bool operator ==(CommandID left, CommandID right) => left.Equals(right);
        public static bool operator !=(CommandID left, CommandID right) => !(left == right);
    }
}
