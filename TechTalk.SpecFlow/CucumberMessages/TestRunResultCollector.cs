using System;
using System.Collections.Generic;
using Io.Cucumber.Messages;
using TechTalk.SpecFlow.CommonModels;

namespace TechTalk.SpecFlow.CucumberMessages
{
    public class TestRunResultCollector : ITestRunResultCollector
    {
        private readonly Dictionary<ScenarioInfo, TestResult> _collectedResults = new Dictionary<ScenarioInfo, TestResult>();

        public bool IsStarted { get; private set; }

        public void StartCollecting()
        {
            if (IsStarted)
            {
                return;
            }

            IsStarted = true;
        }

        public void CollectTestResultForScenario(ScenarioInfo scenarioInfo, TestResult testResult)
        {
            if (!IsStarted)
            {
                throw new InvalidOperationException("Result collection has not been started.");
            }


            lock (_collectedResults)
            {
                _collectedResults.Add(scenarioInfo, testResult);
            }
        }

        public IResult<TestRunResult> GetCurrentResult()
        {
            if (!IsStarted)
            {
                return Result<TestRunResult>.Failure("Result collection has not been started");
            }

            int passedCount = 0;
            int failedCount = 0;
            int skippedCount = 0;
            int ambiguousCount = 0;
            int undefinedCount = 0;
            int resultTotal;

            lock (_collectedResults)
            {
                resultTotal = _collectedResults.Count;
                foreach (var kvp in _collectedResults)
                {
                    switch (kvp.Value.Status)
                    {
                        case TestResult.Types.Status.Passed: passedCount++; break;
                        case TestResult.Types.Status.Failed: failedCount++; break;
                        case TestResult.Types.Status.Skipped: skippedCount++; break;
                        case TestResult.Types.Status.Ambiguous: ambiguousCount++; break;
                        case TestResult.Types.Status.Undefined: undefinedCount++; break;
                    }
                }
            }

            var testRunResult = new TestRunResult(resultTotal, passedCount, failedCount, skippedCount, ambiguousCount, undefinedCount);
            return Result.Success(testRunResult);
        }
    }
}