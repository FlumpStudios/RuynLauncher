using System.IO;
using System.Linq;
using System;
using System.Windows;
namespace RuynLancher
{
    
    [Serializable]
    public class Elements
    {
        public Elements()
        {
            Coords = new byte[2];
        }
        public byte Type;
        public byte[] Coords;
    }

    [Serializable]
    public class Level
    {
        public const int MAX_WALL_SIZE = 31;
        public const int TEXTURE_COUNT = 7;
        public const int SFG_TILE_DICTIONARY_SIZE = 64;
        public const int DOOR_TEXTURE_INDEX = 15;
        public const int MAP_DIMENSION = 64;
        public const byte PLAYER_POSITION_TYPE_INDEX = 99;
        public const int MAX_ELEMENT_SIZE = 128;
        public byte stepSize = 1;

        public byte ceilHeight = 10;
        public byte doorLevitation = 0;
        public byte floorHeight = 10;
        public byte[] HeightArray;

        public Level()
        {
            DoorTextureIndex = DOOR_TEXTURE_INDEX;
            FloorColor = 20;
            CeilingColor = 3;

            // 7 is transparent
            TextureIndices = new byte[7] { 5, 2, 3, 4, 5, 6, 8 };

            HeightArray = new byte[4096];
            MapArray = new byte[4096];

            PlayerStart = new byte[3] { 32, 32, 0 };
            elements = new Elements[MAX_ELEMENT_SIZE];
            BackgroundImage = 1;

            for (int i = 0; i < MAX_ELEMENT_SIZE; i++)
            {
                elements[i] = new Elements
                {
                    Coords = new byte[2],
                    Type = 0
                };
            }

            this.TileDictionary = new ushort[SFG_TILE_DICTIONARY_SIZE];
            GenerateTileDictionary();
        }

        public byte[] MapArray;
        public UInt16[] TileDictionary;
        public byte[] TextureIndices;
        public byte DoorTextureIndex = DOOR_TEXTURE_INDEX;
        public byte FloorColor { get; set; }
        public byte CeilingColor { get; set; }

        public byte[] PlayerStart;
        public byte BackgroundImage;
        public Elements[] elements;

        public static byte[] InvertArrayWidth(byte[] inputArray)
        {
            byte[] outputArray = new byte[inputArray.Length];

            for (int i = 0; i < inputArray.Length; i++)
            {
                int x = i % MAP_DIMENSION;
                int y = i / MAP_DIMENSION;

                // Reverse the order of columns
                int adjustedX = MAP_DIMENSION - 1 - x;

                // Calculate the new index in the output array
                int newIndex = adjustedX + y * MAP_DIMENSION;

                // Copy the value from the input array to the corresponding position in the output array
                outputArray[newIndex] = inputArray[i];
            }

            return outputArray;
        }

        public void Deserialise(BinaryReader br)
        {
            for (int i = 0; i < 4096; i++)
            {
                this.MapArray[i] = br.ReadByte();
            }
            this.MapArray = InvertArrayWidth(this.MapArray);

            for (int i = 0; i < SFG_TILE_DICTIONARY_SIZE; i++)
            {
                this.TileDictionary[i] = br.ReadUInt16();
            }

            for (int i = 0; i < 7; i++)
            {
                this.TextureIndices[i] = br.ReadByte();
            }

            // Read door texture index even though not used
            br.ReadByte();

            this.FloorColor = br.ReadByte();
            this.CeilingColor = br.ReadByte();


            this.PlayerStart[0] = (byte)(MAP_DIMENSION - 1 - br.ReadByte());
            this.PlayerStart[1] = br.ReadByte();
            this.PlayerStart[2] = br.ReadByte();


            this.BackgroundImage = br.ReadByte();

            for (int i = 0; i < MAX_ELEMENT_SIZE; i++)
            {
                this.elements[i].Type = br.ReadByte();
                this.elements[i].Coords[0] = (byte)(MAP_DIMENSION - 1 - br.ReadByte());
                this.elements[i].Coords[1] = br.ReadByte();
            }

            this.ceilHeight = br.ReadByte();
            this.floorHeight = br.ReadByte();

            if (br.BaseStream.Position < br.BaseStream.Length)
            {
                this.doorLevitation = br.ReadByte();
                this.stepSize = br.ReadByte();
            }
            else
            {
                this.doorLevitation = 0;
                this.stepSize = 1;
            }
        }

        public void Serialise(BinaryWriter bw)
        {
            GenerateTileDictionary();

            // Ensure player position isn't saved as an element
            foreach (var element in this.elements)
            {
                if (element.Type == PLAYER_POSITION_TYPE_INDEX)
                {
                    element.Type = 0;
                    element.Coords[0] = 0;
                    element.Coords[1] = 0;
                }
            }

            // Ensure that all elements with a type are at the beginning of the array so can be ignore game/engine side.
            this.elements = this.elements.OrderByDescending(x => x.Type).ToArray();

            var toWrite = InvertArrayWidth(this.MapArray);
            foreach (var maptItem in toWrite)
            {
                bw.Write(maptItem);
            }

            foreach (var tile in this.TileDictionary)
            {
                bw.Write(tile);
            }

            foreach (var textureIndex in this.TextureIndices)
            {
                bw.Write(textureIndex);
            }

            bw.Write(this.DoorTextureIndex);
            bw.Write(this.FloorColor);
            bw.Write(this.CeilingColor);


            bw.Write((byte)(MAP_DIMENSION - 1 - this.PlayerStart[0]));
            bw.Write(this.PlayerStart[1]);
            bw.Write(this.PlayerStart[2]);
            bw.Write(this.BackgroundImage);


            foreach (var element in this.elements)
            {
                bw.Write(element.Type);
                bw.Write((byte)(MAP_DIMENSION - 1 - element.Coords[0]));
                bw.Write(element.Coords[1]);
            }

            bw.Write(ceilHeight);
            bw.Write(floorHeight);
            bw.Write(doorLevitation);
            bw.Write(stepSize);
        }

        internal record GridPosition(int Column, int Row)
        {
            internal int MapArrayIndex()
            {
                var index = (Row * 64) + Column;
                return index;
            }
        };

        internal Elements GetElementAtPosition(GridPosition position)
        {
            var response = new Elements();
            if (elements != null && position != null)
            {
                response = elements.FirstOrDefault(x => x.Coords[0] == position.Column && x.Coords[1] == position.Row) ?? new Elements();
            }
            return response;
        }

        public static ushort SFG_TD(ushort floorH, ushort ceilH, ushort floorT, ushort ceilT)
        {
            return ((ushort)((floorH & 0x001f) | ((floorT & 0x0007) << 5) | ((ceilH & 0x001f) << 8) | ((ceilT & 0x0007) << 13)));
        }

        private void GenerateTileDictionary()
        {
            this.TileDictionary = new ushort[SFG_TILE_DICTIONARY_SIZE];

            {
                // Open space
                this.TileDictionary[0] = SFG_TD(0, ceilHeight, 0, 0); // 0

                // Doors 
                this.TileDictionary[1] = SFG_TD((byte)((doorLevitation * 4) + 4), ceilHeight > 30 ? (ushort)31 : (ushort)0, 0, 0); // 1
                this.TileDictionary[2] = SFG_TD((byte)((doorLevitation * 4) + 4), ceilHeight > 30 ? (ushort)31 : (ushort)0, 1, 1); // 2
                this.TileDictionary[3] = SFG_TD((byte)((doorLevitation * 4) + 4), ceilHeight > 30 ? (ushort)31 : (ushort)0, 2, 2); // 3 
                this.TileDictionary[4] = SFG_TD((byte)((doorLevitation * 4) + 4), ceilHeight > 30 ? (ushort)31 : (ushort)0, 3, 3); // 4
                this.TileDictionary[5] = SFG_TD((byte)((doorLevitation * 4) + 4), ceilHeight > 30 ? (ushort)31 : (ushort)0, 4, 4); // 5
                this.TileDictionary[6] = SFG_TD((byte)((doorLevitation * 4) + 4), ceilHeight > 30 ? (ushort)31 : (ushort)0, 5, 5); // 6
                this.TileDictionary[7] = SFG_TD((byte)((doorLevitation * 4) + 4), ceilHeight > 30 ? (ushort)31 : (ushort)0, 6, 6); // 7

                // Walls
                this.TileDictionary[8] = SFG_TD(floorHeight, ceilHeight, 0, 0); // 8
                this.TileDictionary[9] = SFG_TD(floorHeight, ceilHeight, 1, 1); // 9
                this.TileDictionary[10] = SFG_TD(floorHeight, ceilHeight, 2, 2); // 10
                this.TileDictionary[11] = SFG_TD(floorHeight, ceilHeight, 3, 3); // 11
                this.TileDictionary[12] = SFG_TD(floorHeight, ceilHeight, 4, 4); // 12
                this.TileDictionary[13] = SFG_TD(floorHeight, ceilHeight, 5, 5); // 13
                this.TileDictionary[14] = SFG_TD(floorHeight, ceilHeight, 6, 6); // 14              
            };

            ushort textureIndex = 0;
            ushort heightIndex = this.stepSize;

            // Platforms
            for (int i = 15; i < SFG_TILE_DICTIONARY_SIZE; i++)
            {
                this.TileDictionary[i] = SFG_TD(heightIndex, ceilHeight < MAX_WALL_SIZE ? (ushort)(ceilHeight - heightIndex) : (ushort)MAX_WALL_SIZE, (ushort)(textureIndex), (ushort)(textureIndex)); // 15

                textureIndex++;
                if (textureIndex >= TEXTURE_COUNT)
                {
                    textureIndex = 0;
                    heightIndex += this.stepSize;
                }
            }
        }
    }
}
