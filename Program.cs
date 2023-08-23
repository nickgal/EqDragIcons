using Pfim;
using SixLabors.ImageSharp.Formats.Png;

Image ReadTga(string filepath)
{
    using var image = Pfimage.FromFile(filepath);
    byte[] newData;

    // Since image sharp can't handle data with line padding in a stride
    // we create an stripped down array if any padding is detected
    var tightStride = image.Width * image.BitsPerPixel / 8;
    if (image.Stride != tightStride)
    {
        newData = new byte[image.Height * tightStride];
        for (int i = 0; i < image.Height; i++)
        {
            Buffer.BlockCopy(image.Data, i * image.Stride, newData, i * tightStride, tightStride);
        }
    }
    else
    {
        newData = image.Data;
    }

    return LoadImageData(image, newData);
}

Image LoadImageData(IImage image, byte[] newData)
{
    var encoder = new PngEncoder();

    switch (image.Format)
    {
        case ImageFormat.Rgba32:
            return Image.LoadPixelData<Bgra32>(newData, image.Width, image.Height);
        case ImageFormat.Rgb24:
            return Image.LoadPixelData<Bgr24>(newData, image.Width, image.Height);
        case ImageFormat.Rgba16:
            return Image.LoadPixelData<Bgra4444>(newData, image.Width, image.Height);
        case ImageFormat.R5g5b5:
            // Turn the alpha channel on for image sharp.
            for (int i = 1; i < newData.Length; i += 2)
            {
                newData[i] |= 128;
            }
            return Image.LoadPixelData<Bgra5551>(newData, image.Width, image.Height);
        case ImageFormat.R5g5b5a1:
            return Image.LoadPixelData<Bgra5551>(newData, image.Width, image.Height);
        case ImageFormat.R5g6b5:
            return Image.LoadPixelData<Bgr565>(newData, image.Width, image.Height);
        case ImageFormat.Rgb8:
            return Image.LoadPixelData<L8>(newData, image.Width, image.Height);
        default:
            throw new Exception($"ImageSharp does not recognize image format: {image.Format}");
    }
}

void CreateSpriteSheet(string[] imageFiles, string outputFilepath)
{
    var tgaBase = @"C:\Apps\EverQuest.app\Contents\Resources\UIFiles\default";
    var images = new List<(string, Image)>();

    foreach (var imageFile in imageFiles)
    {
        var imageFilepath = Path.Combine(tgaBase, imageFile);
        images.Add((imageFile, ReadTga(imageFilepath)));
    }

    const int spriteSize = 40;
    var resultImage = new Image<Rgba32>(640, 480);
    for(int x = 0; x < images.Count; x++)
    {
        var image = images[x];
        var sourceImage = image.Item2;
        var sourceImageOffset = spriteSize * x * 6 / 2;

        for (int i = 0; i < 6; i++)
        {
            var pointXOffset = i % 2 == 0 ? i : i - 1;
            var pointYOffset = i % 2 == 0 ? 0 : 6;

            //  split 22 between two sheets
            var drag4 = outputFilepath.Contains("dragitem04");
            var drag5 = outputFilepath.Contains("dragitem05");
            if (image.Item1 == "dragitem22.tga")
            {
                if (drag4 && i > 1)
                {
                    break;
                }
                if (drag5 && i < 2)
                {
                    continue;
                }
            }
            if (drag5)
            {
                pointXOffset -= 2;
            }

            var point = new Point(sourceImageOffset + spriteSize * pointXOffset / 2, spriteSize * pointYOffset);
            var sourceRect = new Rectangle(spriteSize * i, 0, spriteSize, sourceImage.Height);
            // less than optimal
            // https://github.com/SixLabors/ImageSharp/issues/2447
            resultImage.Mutate(x => {
                using var slice = sourceImage.Clone(c => c.Crop(sourceRect));
                x.DrawImage(slice, point, 1f);
            });
        }
    }
    resultImage.SaveAsPng(outputFilepath);
}

string[] dragitem04Files = {
    "dragitem17.tga",
    "dragitem18.tga",
    "dragitem19.tga",
    "dragitem20.tga",
    "dragitem21.tga",
    "dragitem22.tga",
};

string[] dragitem05Files = {
    "dragitem22.tga",
    "dragitem23.tga",
    "dragitem24.tga",
    "dragitem25.tga",
};

CreateSpriteSheet(dragitem04Files, @"C:\Code\EqDragIcons\dragitem04.png");
CreateSpriteSheet(dragitem05Files, @"C:\Code\EqDragIcons\dragitem05.png");
