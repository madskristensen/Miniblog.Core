using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Miniblog.Core.Services
{
    public interface IFileService
    {
        Task<string> SaveFile(byte[] bytes, string fileName, string suffix = null);
    }
}
