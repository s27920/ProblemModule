using System.Diagnostics;

namespace WebApplication17.Executor;

public interface IExecutorService
{
    public Task<ExecuteResultDto> FullExecute(ExecuteRequestDto executeRequestDto);
    public Task<ExecuteResultDto> DryExecute(ExecuteRequestDto executeRequestDto);
}


public class ExecutorService(IExecutorRepository executorRepository) : IExecutorService
{
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
        await execProcess.WaitForExitAsync();
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
        
        await File.WriteAllTextAsync(fileData.FilePath, executeRequest.Code);
        
        // InsertTestCases(fileData.FilePath, funcName);
        return fileData;
    }
    

    private bool ValidateFileContents()
    {
        return true;
        //TODO for now let's all pass, gonna use AST later on.
    }

    private async Task InsertTestCases(FileData fileData)
    {
        // TODO fetch test cases, for now hardcoded
        /*
         proposed test case format
         test data<
         expected output<<
         test data<
         expected output<<
         ...
         Could also have them all in one line beats me, less space but less readable.
         Furthermore enumerateTestCases would offset by 1 and 2 respectively instead of 2 and 3
         */
        var testCases = "[1,5,2,4,3]<\n" +
                   "[1,2,3,4,5]<<\n" +
                   "[94,37,9,52,17]<\n" +
                   "[9,17,37,52,94]<<\n" ;

        await using var fileWriter = new StreamWriter(fileData.FilePath, true);
        
        foreach (var testCase in EnumerateTestCases(testCases))
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

    IEnumerable<TestCase> EnumerateTestCases(string testCases)
    {
        for (var i = 0; i < testCases.Length;)
        {
            var endOfTest = testCases.IndexOf('<', i);
            var str1 = testCases.Substring(i, endOfTest - i);
            i = endOfTest + 2;

            var endOfCorr = testCases.IndexOf("<<", i, StringComparison.Ordinal);
            var str2 = testCases.Substring(i, endOfCorr - i);
            i = endOfCorr + 3;
            yield return new TestCase(str1, str2);
        }
    }

    private class FileData(Guid guid, string lang, string funcName)
    {
        public Guid Guid => guid;

        public string Lang => lang;

        public string FuncName => funcName;
        
        public string FilePath => $"client-src/{lang}/{guid.ToString()}.java";
    }

}