using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gengar.Models.Mongo
{
    [BsonIgnoreExtraElements]
    public class Birthdays
    {
        [BsonRepresentation(BsonType.Int64)]
        public ulong _id { get; set; }
        [BsonRepresentation(BsonType.DateTime)]
        [BsonElement("birthday")]
        public DateTime Birthday { get; set; }
    }
}
