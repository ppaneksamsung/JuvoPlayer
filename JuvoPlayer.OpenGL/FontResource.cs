﻿namespace JuvoPlayer.OpenGL
{
    class FontResource : Resource
    {
        private int _id;
        private string _path;
        private byte[] _data;

        public FontResource(string path) : base()
        {
            _path = path;
        }

        public override void Load()
        {
            _data = GetData(_path);
        }

        public override unsafe void Push()
        {
            fixed (byte* p = _data)
            {
                _id = DllImports.AddFont(p, _data.Length);
            }
        }
    }
}
