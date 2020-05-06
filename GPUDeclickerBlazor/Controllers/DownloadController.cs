using GPUDeclickerBlazor.Data;
using AudioInputOutput_NAudio;
using Microsoft.AspNetCore.Mvc;
using System.IO;

[ApiController, Route("api/[controller]")]
public class DownloadController : ControllerBase
{
    [HttpGet, Route("{name}")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
    public ActionResult Get(string name, [FromServices] AppState appState)
    {
        if (appState is null || appState.Audio is null)
            return new EmptyResult();

        var audioIO = new AudioInputOutput();
        audioIO.SetAudioData(appState.Audio);
        var (success, stream) = audioIO.SaveAudioToStream();

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