using System.Diagnostics;
using System.Text;
using Microsoft.Win32.SafeHandles;
using Polly;
using Polly.Timeout;
using WebApplication17.Analyzer._AnalyzerUtils;
using WebApplication17.Analyzer.AstAnalyzer;

namespace WebApplication17.Executor;

public interface IExecutorService
{
    public Task<ExecuteResultDto> FullExecute(ExecuteRequestDto executeRequestDto);
    public Task<ExecuteResultDto> DryExecute(ExecuteRequestDto executeRequestDto);
}


public class ExecutorService(IExecutorRepository executorRepository, IExecutorConfig executorConfig) : IExecutorService
{
    private const string JavaImport = "import com.google.gson.Gson;\n\n"; //TODO this is temporary, not the gson but the way it's imported
    
    private readonly IExecutorConfig _config = executorConfig; //TODO use this to check language selection
    private IAnalyzer? _analyzer;
    private CodeAnalysisResult? _codeAnalysisResult;

    public async Task<ExecuteResultDto> FullExecute(ExecuteRequestDto executeRequestDto)
    {
        SrcFileData fileData = await PrepareFile(executeRequestDto);

        _analyzer = new AnalyzerSimple(await File.ReadAllTextAsync(fileData.FilePath), await executorRepository.GetTemplateAsync());
        
        _codeAnalysisResult = _analyzer.AnalyzeUserCode();
        
        if (!_codeAnalysisResult.PassedValidation)
        {
            return new ExecuteResultDto("", "critical function signature modified. Exiting.");
        }
        
        if (_codeAnalysisResult.MainMethodIndices is not null)
        {
            await InsertTestCases(fileData, _codeAnalysisResult.MainMethodIndices.MethodFileEndIndex);
        }
        else
        {
            //TODO temporary solution I'd like to insert a main if it's not found to test either way
            return new ExecuteResultDto("", "no main found. Exiting");
        }

        // return new ExecuteResultDto($"{await File.ReadAllTextAsync(fileData.FilePath)}", ""); /*[DEBUG]*/
        return (await Exec(fileData));
    }

    public async Task<ExecuteResultDto> DryExecute(ExecuteRequestDto executeRequestDto)
    {
        _analyzer = new AnalyzerSimple(executeRequestDto.Code);
        _codeAnalysisResult = _analyzer.AnalyzeUserCode();
        
        var fileData = await PrepareFile(executeRequestDto);
        // return new ExecuteResultDto(await File.ReadAllTextAsync(fileData.FilePath), ""); /*[DEBUG]*/
        return await Exec(fileData);
    }

    private async Task<ExecuteResultDto> Exec(SrcFileData srcFileData)
    {
        
        var execProcess = new Process()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = "/bin/sh",
                Arguments = $"\"./scripts/deploy-executor-container.sh\" \"{srcFileData.Lang}\" \"{srcFileData.Guid.ToString()}\" \"{_codeAnalysisResult!.MainClassName}\"", //never actually gonna be null which is why I threw that ! in
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                CreateNoWindow = true
            }
        };

        execProcess.Start();
        
        await execProcess.StandardInput.WriteAsync(await File.ReadAllTextAsync(srcFileData.FilePath));
        execProcess.StandardInput.Close();

        var timeoutPolicy = Policy.TimeoutAsync(TimeSpan.FromSeconds(30), TimeoutStrategy.Pessimistic);
        try // handles spinlocks
        {
            await timeoutPolicy.ExecuteAsync(async token => await execProcess.WaitForExitAsync(token), CancellationToken.None);
        }
        catch (TimeoutRejectedException)
        {
            var timeoutProcess = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "/bin/sh",
                    Arguments = $"\"./scripts/timeout-container.sh\" \"{srcFileData.Lang}-{srcFileData.Guid.ToString()}\"",
                    CreateNoWindow = true
                }
            };
            timeoutProcess.Start();
            await timeoutProcess.WaitForExitAsync();
            return new ExecuteResultDto("", "executing timed out. Aborting");
        }
        
        File.Delete(srcFileData.FilePath);

        var output = await execProcess.StandardOutput.ReadToEndAsync();
        var error = await execProcess.StandardError.ReadToEndAsync();
        
        return new ExecuteResultDto(output, error);
    }

    private async Task<SrcFileData> PrepareFile(ExecuteRequestDto executeRequest) //TODO Make the import not constant
    {
        string funcName = GetFuncSignature();
        
        if (!ValidateFileContents())
        {
            throw new FunctionSignatureException(null);
        }

        var fileData = new SrcFileData(Guid.NewGuid(), executeRequest.Lang, funcName);

        await File.WriteAllTextAsync(fileData.FilePath, JavaImport);
        await File.AppendAllTextAsync(fileData.FilePath, executeRequest.Code);
        
        return fileData;
    }
    

    private bool ValidateFileContents()
    {
        return true;
        //TODO for now let's all pass, gonna use AST later on.
    }

    private async Task InsertTestCases(SrcFileData srcFileData, int writeOffset)
    {
        TestCase[] testCases = await executorRepository.GetTestCasesAsync();

        using SafeFileHandle handle = File.OpenHandle(srcFileData.FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

        long len = new FileInfo(srcFileData.FilePath).Length;
        byte[] fileTail = new byte[len - writeOffset];
        await RandomAccess.ReadAsync(handle, fileTail, writeOffset);
        
        StringBuilder testCaseInsertBuilder = new StringBuilder();
        testCaseInsertBuilder.Append("Gson gson = new Gson();\n");

        foreach (var testCase in testCases)
        {
            string comparingStat = "System.out.println(\"hello from comparer\");\n";
            testCaseInsertBuilder.Append(comparingStat);
            
            //TODO add actual comparisons
            // string comparingStatement = "System.out.println(gson.toJson(\"hello from comparer\"));\n"u8.ToArray();
        }
        
        byte[] insertionBytes = Encoding.UTF8.GetBytes(testCaseInsertBuilder.ToString());
        byte[] combinedBytes = insertionBytes.Concat(fileTail).ToArray();
        
        await RandomAccess.WriteAsync(handle, combinedBytes, writeOffset);
    }

    private string GetFuncSignature()
    {
        // TODO fetch and parse file template, for now hardcoded
        string funcName = "func";
        return funcName;
    }
}