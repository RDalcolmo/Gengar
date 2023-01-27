using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

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

        public short CurrentDay { get; set; }
    }
}
