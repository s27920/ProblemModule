using System.Diagnostics;
using Polly;
using Polly.Timeout;

namespace WebApplication17.Executor;

public interface IExecutorService
{
    public Task<ExecuteResultDto> FullExecute(ExecuteRequestDto executeRequestDto);
    public Task<ExecuteResultDto> DryExecute(ExecuteRequestDto executeRequestDto);
}


public class ExecutorService(IExecutorRepository executorRepository) : IExecutorService
{
    private const string javaImport = "import com.google.gson.Gson;\n"; //TODO this is temporary, not the gson but the way it's imported
    
    private readonly ExecutorConfig _config = new ExecutorConfig(); //TODO use this to check language selection
    private readonly IExecutorRepository _executorRepository = executorRepository;
    public async Task<ExecuteResultDto> FullExecute(ExecuteRequestDto executeRequestDto)
    {
        var fileData = await PrepareFile(executeRequestDto);
        await InsertTestCases(fileData);
        return (await Exec(fileData));
    }

    public async Task<ExecuteResultDto> DryExecute(ExecuteRequestDto executeRequestDto)
    {
        var fileData = await PrepareFile(executeRequestDto);
        return await Exec(fileData);
    }

    private async Task<ExecuteResultDto> Exec(FileData fileData)
    {
        var fileContests = await File.ReadAllTextAsync(fileData.FilePath);

        var execProcess = new Process()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = "/bin/sh",
                Arguments = $"\"./scripts/deploy-executor-container.sh\" \"{fileData.Lang}\" \"{fileData.Guid.ToString()}\"",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                CreateNoWindow = true
            }
        };

        execProcess.Start();
        
        await execProcess.StandardInput.WriteAsync(fileContests);
        execProcess.StandardInput.Close();

        var timeoutPolicy = Policy.TimeoutAsync(TimeSpan.FromSeconds(5), TimeoutStrategy.Pessimistic);
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
                    Arguments = $"\"./scripts/timeout-container.sh\" \"{fileData.Lang}-{fileData.Guid.ToString()}\"",
                    CreateNoWindow = true
                }
            };
            timeoutProcess.Start();
            await timeoutProcess.WaitForExitAsync();
            return new ExecuteResultDto("", "executing timed out. Aborting");
        }


        File.Delete(fileData.FilePath);

        var output = await execProcess.StandardOutput.ReadToEndAsync();
        var error = await execProcess.StandardError.ReadToEndAsync();
        
        return new ExecuteResultDto(output, error);
    }

    private async Task<FileData> PrepareFile(ExecuteRequestDto executeRequest)
    {
        string funcName = GetFuncSignature();
        
        if (!ValidateFileContents())
        {
            throw new FunctionSignatureException(null);
        }

        var fileData = new FileData(Guid.NewGuid(), executeRequest.Lang, funcName);

        await File.WriteAllTextAsync(fileData.FilePath, javaImport);
        await File.WriteAllTextAsync(fileData.FilePath, executeRequest.Code);
        
        return fileData;
    }
    

    private bool ValidateFileContents()
    {
        return true;
        //TODO for now let's all pass, gonna use AST later on.
    }

    private async Task InsertTestCases(FileData fileData)
    {
        await using var fileWriter = new StreamWriter(fileData.FilePath, true);
        
        foreach (var testCase in await _executorRepository.GetTestCasesAsync())
        {
            await fileWriter.WriteLineAsync(PrintComparingStatement(testCase, fileData.FuncName));
        }
    }

    private string GetFuncSignature()
    {
        // TODO fetch and parse file template, for now hardcoded
        string funcName = "func";
        return funcName;
    }

    private string GetComparingStatement(TestCase testCase, string funcName)
    {
        return $"JSON.stringify({testCase.ExpectedOutput}) === JSON.stringify({funcName}({testCase.TestInput}))";
    }

    private string PrintComparingStatement(TestCase testCase, string funcName)
    {
        //TODO change this for java
        return $"\nconsole.log({GetComparingStatement(testCase, funcName)});";
    }
    
    private class FileData(Guid guid, string lang, string funcName)
    {
        public Guid Guid => guid;

        public string Lang => lang;

        public string FuncName => funcName;
        
        public string FilePath => $"client-src/{lang}/{guid.ToString()}.java";
    }
    
}