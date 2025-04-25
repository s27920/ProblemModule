namespace WebApplication17.Executor;

public interface IExecutorRepository
{
    public Task<Language[]> GetSupportedLangsAsync();
    public Language[] GetSupportedLangs();
    public Task<TestCase[]> GetTestCasesAsync();
    public Task<TestCase> GetTemplateAsync();
}

public class ExecutorRepository : IExecutorRepository
{
    public async Task<Language[]> GetSupportedLangsAsync()
    {
        throw new NotImplementedException();
    }

    public Language[] GetSupportedLangs()
    {
        throw new NotImplementedException();
    }

    public async Task<TestCase[]> GetTestCasesAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<TestCase> GetTemplateAsync()
    {
        throw new NotImplementedException();
    }
}
public class ExecutorRepositoryMock : IExecutorRepository
{
    public async Task<Language[]> GetSupportedLangsAsync()
    {
        throw new NotImplementedException();
    }

    public Language[] GetSupportedLangs()
    {
        //TODO hardcoded for now
        Language[] languages = [new Language("java", null)];
        return languages;
    }

    public async Task<TestCase[]> GetTestCasesAsync()
    {
        var testCases = "[1,5,2,4,3]<\n" +
                        "[1,2,3,4,5]<<\n" +
                        "[94,37,9,52,17]<\n" +
                        "[9,17,37,52,94]<<\n";
        return ParseTestCases(testCases);
    }

    public async Task<TestCase> GetTemplateAsync()
    {
        //not useful for now either way, since no ast gen
        throw new NotImplementedException();
    }
    
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
    
    
    TestCase[] ParseTestCases(string testCases)
    {
        var testCaseList = new List<TestCase>();
        for (var i = 0; i < testCases.Length;)
        {
            var endOfTest = testCases.IndexOf('<', i);
            var testInput = testCases.Substring(i, endOfTest - i);
            i = endOfTest + 2;
            
            var endOfCorr = testCases.IndexOf("<<", i, StringComparison.Ordinal);
            var testOutput = testCases.Substring(i, endOfCorr - i);
            i = endOfCorr + 3;
            
            testCaseList.Add(new TestCase(testInput, testOutput));
        }

        return testCaseList.ToArray();
    }
    
    //TODO cool funky version below boring practical version above
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
    
    
}