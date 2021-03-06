﻿using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Responses;

namespace Ocelot.Headers
{
    public interface IAddHeadersToRequest
    {
        Response SetHeadersOnContext(List<ClaimToThing> claimsToThings,
            HttpContext context);
    }
}
