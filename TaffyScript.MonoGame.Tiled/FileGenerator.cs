using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Myst.Tiled;

namespace TaffyScript.MonoGame.Tiled
{
    public class FileGenerator
    {
        private int _indent = 0;
        private int _sets = 0;
        private int _depth = -100;
        private StreamWriter _file;
        private Dictionary<TiledTileset, string> _loadedTilesets = new Dictionary<TiledTileset, string>();

        public FileGenerator()
        {
        }

        public void Generate(TiledMap map, string saveLocation)
        {
            _loadedTilesets.Clear();
            _depth = -100;
            _sets = 0;
            _indent = 0;
            _file = new StreamWriter(saveLocation);
            var ns = GetMapNamespace(map);
            Write("using TaffyScript.MonoGame;");
            Write(null);

            if (ns != null)
                Write($"namespace {ns} {{");
            else
                Write(null);

            _indent = 4;
            Write($"script tiled_load_{Path.GetFileNameWithoutExtension(saveLocation)}() {{");
            _indent = 8;
            Write("var inst;");
            Write("var screen = new Screen();");
            foreach (var layer in map.Layers)
            {
                ProcessLayer(layer, map);
                _depth++;
            }

            foreach (var objectGroup in map.Objects)
                ProcessObjectGroup(objectGroup);

            foreach (var group in map.Groups)
                ProcessGroup(group, map);

            Write("return screen;");
            _indent = 4;
            Write("}");
            if(ns != null)
            {
                _indent = 0;
                Write("}");
            }
            _file.Flush();
        }

        private void ProcessGroup(TiledGroup group, TiledMap map)
        {
            foreach (var layer in group.Layers)
            {
                ProcessLayer(layer, map);
                _depth++;
            }

            foreach (var objectGroup in group.Objects)
                ProcessObjectGroup(objectGroup);

            foreach (var child in group.Groups)
                ProcessGroup(child, map);
        }

        private void ProcessLayer(TiledLayer layer, TiledMap map)
        {
            var data = layer.Data;
            foreach(var chunk in data.Chunks)
            {
                for(var i = 0; i < chunk.Tiles.Count; i++)
                {
                    var tile = chunk.Tiles[i];
                    if (tile.Gid == 0)
                        continue;
                    var set = GetTilesetFromGid(tile, map);
                    var x = (i % chunk.Width);
                    var y = (i - x) / chunk.Width;
                    x = (x + chunk.X) * set.TileWidth + layer.OffsetX + set.Offset.XOffset;
                    y = (y + chunk.Y) * set.TileHeight + layer.OffsetY + set.Offset.YOffset;
                    var setPos = (int)tile.Gid - set.FirstGid;
                    var bgx = setPos % set.Columns;
                    var bgy = (setPos - bgx) / set.Columns;
                    bgx = bgx * (set.Spacing + set.TileWidth) + set.Margin;
                    bgy = bgy * (set.Spacing + set.TileHeight) + set.Margin;
                    var name = GetTilesetName(set);
                    Write($"screen.add_tile({name}, {bgx}, {bgy}, {set.TileWidth}, {set.TileHeight}, {x}, {y}, {_depth});");
                }
            }

            for(var i = 0; i < data.Tiles.Count; i++)
            {
                var tile = data.Tiles[i];
                if (tile.Gid == 0)
                    continue;
                var set = GetTilesetFromGid(tile, map);
                var x = (i % map.Width);
                var y = ((i - x) / map.Width);
                x = x * set.TileWidth + layer.OffsetX + set.Offset.XOffset;
                y = y * set.TileHeight + layer.OffsetY + set.Offset.YOffset;
                var setPos = (int)tile.Gid - set.FirstGid;
                var bgx = setPos % set.Columns;
                var bgy = (setPos - bgx) / set.Columns;
                bgx = bgx * (set.Spacing + set.TileWidth) + set.Margin;
                bgy = bgy * (set.Spacing + set.TileHeight) + set.Margin;
                var name = GetTilesetName(set);
                Write($"screen.add_tile({name}, {bgx}, {bgy}, {set.TileWidth}, {set.TileHeight}, {x}, {y}, {_depth});");
            }
        }

        private void ProcessObjectGroup(TiledObjectGroup group)
        {
            foreach(var obj in group.Objects)
            {
                var x = (obj.X ?? 0) + group.OffsetX;
                var y = (obj.Y ?? 0) + group.OffsetY;

                if (obj.Properties.Count == 0)
                    Write($"screen.add(new {obj.Type}({x}, {y}, {obj.Width ?? 1}, {obj.Height ?? 1}));");
                else
                {
                    Write($"inst = new {obj.Type}({x}, {y}, {obj.Width ?? 1}, {obj.Height ?? 1});");
                    foreach(var prop in obj.Properties)
                    {
                        string value;
                        if (prop.Type == PropertyType.Color)
                        {
                            var color = (TiledColor)prop.Value;
                            value = $"make_color({color.Red}, {color.Green}, {color.Blue}, {color.Alpha})";
                        }
                        else
                            value = prop.Value.ToString();
                        Write($"inst.{prop.Name} = {value};");
                    }
                    Write("screen.add(inst);");
                }
            }
        }

        private void Write(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                _file.WriteLine();
            else
            {
                _file.Write(new string(' ', _indent));
                _file.WriteLine(line);
            }
        }

        private string GetTilesetName(TiledTileset set)
        {
            if (!_loadedTilesets.TryGetValue(set, out var name))
            {
                var load = (string)set.Properties.FirstOrDefault(p => p.Name == "fname")?.Value ?? set.Name;
                name = $"set{_sets++}";
                Write($"var {name} = load_texture(\"{load}\");");
                _loadedTilesets.Add(set, name);
            }
            return name;
        }

        private string GetMapNamespace(TiledMap map)
        {
            return map.Properties.FirstOrDefault(p => p.Name == "namespace")?.Value as string;
        }

        private TiledTileset GetTilesetFromGid(TiledDataTile tile, TiledMap map)
        {
            var current = map.Tilesets.First();
            for(var i = 1; i < map.Tilesets.Count; i++)
            {
                if (map.Tilesets[i].FirstGid <= tile.Gid)
                    current = map.Tilesets[i];
                else
                    break;
            }

            return current;
        }
    }
}
