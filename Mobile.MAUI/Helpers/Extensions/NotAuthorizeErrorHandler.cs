using Microsoft.AspNetCore.Components;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobile.MAUI.Helpers.Extensions;

public static class NotAuthorizeErrorHandler
{
    public static async Task HandleAction(this ComponentBase componentBase)
    {
        try
        {

        }
        catch (SecurityTokenExpiredException e)
        {

        }

    }
}
