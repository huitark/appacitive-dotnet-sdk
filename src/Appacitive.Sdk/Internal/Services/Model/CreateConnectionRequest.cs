﻿using Appacitive.Sdk.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Appacitive.Sdk.Services
{
    public class CreateConnectionRequest : PutOperation<CreateConnectionResponse>
    {
        public APConnection Connection { get; set; }

        public override byte[] ToBytes()
        {
            var serializer = ObjectFactory.Build<IJsonSerializer>();
            return serializer.Serialize(this.Connection);
        }

        public override async Task<CreateConnectionResponse> ExecuteAsync()
        {
            var response = await base.ExecuteAsync();
            if (response.Status.IsSuccessful == false)
                throw response.Status.ToFault();
            // Update the ids if any new objects were passed in the this.
            if (this.Connection.Endpoints.EndpointA.CreateEndpoint == true)
            {
                if (this.Connection.Endpoints.EndpointA.Label == this.Connection.Endpoints.EndpointB.Label)
                    this.Connection.Endpoints.EndpointA.Content.Id = response.Connection.Endpoints.EndpointA.ObjectId;
                else
                {
                    var match = response.Connection.Endpoints[this.Connection.Endpoints.EndpointA.Label];
                    this.Connection.Endpoints.EndpointA.Content.Id = match.ObjectId;
                }
            }
            if (this.Connection.Endpoints.EndpointB.CreateEndpoint == true)
            {
                if (this.Connection.Endpoints.EndpointA.Label == this.Connection.Endpoints.EndpointB.Label)
                    this.Connection.Endpoints.EndpointB.Content.Id = response.Connection.Endpoints.EndpointB.ObjectId;
                else
                {
                    var match = response.Connection.Endpoints[this.Connection.Endpoints.EndpointB.Label];
                    this.Connection.Endpoints.EndpointB.Content.Id = match.ObjectId;
                }
            }
            return response;
        }

        protected override string GetUrl()
        {
            return Urls.For.CreateConnection(this.Connection.Type, this.CurrentLocation, this.DebugEnabled, this.Verbosity, this.Fields);
        }
    }
}
