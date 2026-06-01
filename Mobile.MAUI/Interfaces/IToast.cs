using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobile.MAUI.Interfaces;

public interface IToast : IDisposable
{
    public void Success(string message);
    public void Success(string message, string title);
    public void Info(string message);
    public void Info(string message, string title);
    public void Warning(string message);
    public void Warning(string message, string title);
    public void Error(string message);
    public void Error(string message, string title);
    public void Clear();
}
