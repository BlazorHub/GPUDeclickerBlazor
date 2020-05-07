using AudioClickRepair.Data;
using AudioInputOutput_NAudio;
using GPUDeclickerBlazor.Data;
using Microsoft.AspNetCore.Mvc;
using System.IO;

[ApiController, Route("api/[controller]")]
public class DownloadController : ControllerBase
{
    [HttpGet, Route("{name}")]
    public ActionResult Get(string name, [FromServices] AppState appState)
    {
        if (appState is null || appState.Audio is null)
            return new EmptyResult();

        var audioIO = new AudioInputOutput();
        var data = new AudioData(
            appState.Audio.Settings.SampleRate,
            appState.Audio.GetOutputArray(ChannelType.Left),
            appState.Audio.GetOutputArray(ChannelType.Right));

        var (success, stream) = audioIO.SaveToMemoryStream(data);

        if (!success)
            return new EmptyResult();

        // Create a new stream as WaveFileWriter closed memoryStream
        var outStream = new MemoryStream(stream.ToArray());

        var result = new FileStreamResult(outStream, "audio/wave")
        {
            FileDownloadName = name + ".wav"
        };

        return result;
    }
}