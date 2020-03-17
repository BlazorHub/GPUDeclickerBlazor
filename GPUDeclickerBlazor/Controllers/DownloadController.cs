using GPUDeclickerBlazor.Data;
using GPUDeclickerUWP.Model.InputOutput;
using Microsoft.AspNetCore.Mvc;
using System.IO;

[ApiController, Route("api/[controller]")]
public class DownloadController : ControllerBase
{
    [HttpGet, Route("{name}")]
    public ActionResult Get(string name, [FromServices] AppState appState)
    {
        if (appState is null || appState.AudioData is null)
            return new EmptyResult();

        var audioIO = new AudioInputOutput();
        audioIO.SetAudioData(appState.AudioData);
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