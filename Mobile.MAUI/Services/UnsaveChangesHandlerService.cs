using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobile.MAUI.Services;

public class UnsaveChangesHandlerService
{
    public bool HasUnsavedChanges { get; set; } = false;
    public void MarkAsClean()
    {
        HasUnsavedChanges = false;
    }
    public void MarkAsDirty()
    {
        HasUnsavedChanges = true;
    }
}
