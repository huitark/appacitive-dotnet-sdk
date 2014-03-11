﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using Appacitive.Sdk.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Appacitive.Sdk.Services
{   
    public class ObjectConverter : EntityConverter
    {
        public override bool CanConvert(Type objectType)
        {
            // Type should not be a User or Device since these have their specific serializers.
            // This serializer should be used for any other type that inherits from object.
            if (objectType != typeof(APUser) && objectType != typeof(APDevice))
                return objectType.Is<APObject>();
            else 
                return false;
        }

        protected override Entity CreateEntity(JObject json)
        {
            JToken value;
            if (json.TryGetValue("__type", out value) == false || value.Type == JTokenType.Null)
                throw new Exception("Schema type missing.");
            var type = value.ToString();
            return new APObject(type);
        }

        protected override Entity ReadJson(Entity entity, Type objectType, JObject json, JsonSerializer serializer)
        {
            if (json == null || json.Type == JTokenType.Null)
                return null;
            // JToken value;
            var obj = base.ReadJson(entity, objectType, json, serializer) as APObject;
            if (obj != null)
            {
                //// Schema Id
                //if (json.TryGetValue("__schemaid", out value) == true && value.Type != JTokenType.Null)
                //    obj.SchemaId = value.ToString();
            }

            // Check for inheritance.
            if (obj.Type.Equals("user", StringComparison.OrdinalIgnoreCase) == true)
                return new APUser(obj);
            else if (obj.Type.Equals("device", StringComparison.OrdinalIgnoreCase) == true)
                return new APDevice(obj);
            else return obj;
        }

        protected override void WriteJson(Entity entity, JsonWriter writer, JsonSerializer serializer)
        {
            if (entity == null)
                return;
            var obj = entity as APObject;
            if (obj != null)
            {
                writer
                    .WriteProperty("__type", obj.Type);
                    //.WriteProperty("__schemaid", obj.SchemaId);
            }
        }

        private static readonly Dictionary<string, bool> _internal = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            {"__schematype", true},
            {"__schemaid", true}
        };

        protected override bool IsSytemProperty(string property)
        {
            if (base.IsSytemProperty(property) == true)
                return true;
            else
                return _internal.ContainsKey(property);
            
        }
    }
}