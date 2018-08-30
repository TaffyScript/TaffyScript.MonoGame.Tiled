using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Myst.Tiled;

namespace TaffyScript.MonoGame.Tiled
{
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            var mapLocation = @"C:\Users\Chris\Desktop\Tiled\maps\test.tmx";
            var output = @"C:\Users\Chris\Desktop\Tiled\maps
#else
            var mapLocation = args[0];
            var output = args.Length > 1 ? args[1] : Path.GetDirectoryName(mapLocation);
#endif
            var saveLocation = Path.Combine(output, Path.GetFileNameWithoutExtension(mapLocation) + ".tfs");
            var map = new TiledMap(mapLocation);
            var generator = new FileGenerator();
            generator.Generate(map, saveLocation);
            Console.WriteLine($"Output: {saveLocation}");
        }
    }
}