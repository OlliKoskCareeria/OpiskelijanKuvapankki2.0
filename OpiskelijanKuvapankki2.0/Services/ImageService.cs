using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace OpiskelijanKuvapankki2_0.Services
{
    public class ImageService
    {
        public async Task<byte[]> DowngradeImageAsync(Stream imageStream)
        {
            using var image = await Image.LoadAsync(imageStream);

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(700, 700),//muutetaan kuvan koko vakioituun muotoon
                Mode = ResizeMode.Max //Pidetään kuvasuhteet ennallaan
            }));

            var encoder = new JpegEncoder
            {
                Quality = 50 //muutetaan kuvan laatu vakioituun muotoon
            };

            using var processedImageStream = new MemoryStream();
            await image.SaveAsJpegAsync(processedImageStream, encoder);//tallennetaan jpeg muodossa

            return processedImageStream.ToArray();
        }

        public byte[] UpGradeImage(byte[] imageAsBytes)
        {
            using (var image = Image.Load(imageAsBytes))
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                { Size = new Size(1980, 1080) }));

                var encoder = new JpegEncoder { Quality = 100 };//parannetaan kuvan laatua

                using (var outputStream = new MemoryStream())
                {
                    image.Save(outputStream, encoder);
                    return outputStream.ToArray();
                }
            }

        }
    }
}
