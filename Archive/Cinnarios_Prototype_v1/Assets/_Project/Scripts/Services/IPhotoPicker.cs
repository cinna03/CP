using System;
using UnityEngine;

namespace Capstone.App.Services
{
    /// <summary>Lets the buyer choose one photo. The callback gets null if they cancel or the photo cannot be read.</summary>
    public interface IPhotoPicker
    {
        void Pick(Action<Texture2D> done);
    }
}
