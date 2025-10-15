# CMAB Testing Action Plan for C# SDK

## Executive Summary
This document outlines a comprehensive testing strategy for CMAB (Contextual Multi-Armed Bandit) implementation in the C# SDK, based on analysis of Swift SDK PR #602 test coverage and existing C# test patterns.

**Key Insight:** CMAB tests do NOT require conditional compilation directives (`#if USE_CMAB`). The test project targets .NET 4.5 where CMAB is always available, following the same pattern as existing ODP tests.

---

## Phase 1: Understanding Swift Test Coverage

### 1.1 Swift Test Files Added for CMAB
From Swift PR #602, the following test files were added:

1. **BucketTests_BucketToEntity.swift** - New bucketing method tests
2. **CmabServiceTests.swift** - CMAB service unit tests
3. **OptimizelyUserContextTests_Decide_CMAB.swift** - Integration tests for CMAB decisions
4. **OptimizelyUserContextTests_Decide_Async.swift** - Async decision tests (Note: C# is synchronous)

### 1.2 Modified Test Files
- **DecisionServiceTests_Experiments.swift** - Added CMAB experiment tests
- **DecisionListenerTests.swift** - Added CMAB notification tests

### 1.3 Swift Test Coverage Areas

#### A. **Bucketing Tests** (`BucketTests_BucketToEntity.swift`)
- ✅ Test `bucketToEntityId()` method
- ✅ Bucket to CMAB dummy entity ID
- ✅ Traffic allocation for CMAB experiments
- ✅ Mutex rule checking for CMAB experiments
- ✅ Zero traffic allocation handling
- ✅ Hash generation and bucket value calculation

#### B. **CMAB Service Tests** (`CmabServiceTests.swift`)
**Cache Management:**
- ✅ Cache hit with matching attributes hash
- ✅ Cache miss when attributes change
- ✅ Cache invalidation options (`ignoreCmabCache`, `resetCmabCache`, `invalidateUserCmabCache`)
- ✅ Cache key generation

**Attribute Filtering:**
- ✅ Filter attributes based on experiment's `attributeIds`
- ✅ Hash attributes deterministically (same attrs = same hash, different order)
- ✅ Handle missing attributes gracefully

**API Integration:**
- ✅ Fetch decision from CMAB client
- ✅ Handle CMAB client errors
- ✅ Retry logic and error propagation
- ✅ UUID generation and caching

**Synchronous/Asynchronous:**
- ✅ Synchronous decision (C# equivalent)
- ✅ Async decision with completion handler (Not needed for C#)

#### C. **Decision Service Tests** (`DecisionServiceTests_Experiments.swift`)
**CMAB Experiment Flow:**
- ✅ `GetVariation()` with CMAB traffic allocation
- ✅ CMAB experiment with zero traffic allocation
- ✅ CMAB not supported in sync mode (C# is always sync)
- ✅ User bucketed into CMAB variation
- ✅ CMAB service error handling
- ✅ Fall back to standard bucketing on CMAB failure

**Traffic Allocation:**
- ✅ Full traffic allocation (10000)
- ✅ Zero traffic allocation (0)
- ✅ Partial traffic allocation

#### D. **User Context Decide Tests** (`OptimizelyUserContextTests_Decide_CMAB.swift`)
**Basic CMAB Decision:**
- ✅ `Decide()` with CMAB experiment
- ✅ Variation key returned correctly
- ✅ CMAB UUID generated and returned
- ✅ Feature enabled status
- ✅ Variables resolved correctly

**Impression Events:**
- ✅ Impression event sent with CMAB UUID
- ✅ Event metadata includes `cmab_uuid` field
- ✅ Rule type and flag key in metadata
- ✅ Variation key in metadata

**Multiple Decisions:**
- ✅ `DecideForKeys()` with mixed CMAB/non-CMAB experiments
- ✅ CMAB service called correct number of times
- ✅ Cache shared across multiple decisions

**Cache Options:**
- ✅ User profile service caching (skip with `ignoreUserProfileService`)
- ✅ CMAB cache options (`ignoreCmabCache`, `resetCmabCache`, `invalidateUserCmabCache`)
- ✅ Cache behavior with repeated decisions

**Error Handling:**
- ✅ CMAB service returns error
- ✅ Decision reasons include CMAB error message
- ✅ Fallback to null variation on error

**Notification Tests:**
- ✅ Decision notification includes CMAB UUID
- ✅ Flag decision notification
- ✅ Impression event notification

---

## Phase 2: C# Test Structure Analysis

### 2.1 Test Framework
- **Framework:** NUnit
- **Mocking:** Moq library
- **Assertions:** Custom `Assertions` class + NUnit Assert

### 2.2 Test Organization Pattern
```
OptimizelySDK.Tests/
├── BucketerTest.cs                    # Bucketing logic tests
├── DecisionServiceTest.cs             # Decision service tests
├── OptimizelyUserContextTest.cs       # User context tests
├── CmabTests/                         # CMAB-specific tests
│   ├── DefaultCmabServiceTest.cs      # Service layer tests
│   └── DefaultCmabClientTest.cs       # Client layer tests
└── TestData/                          # Test datafiles
```

### 2.2.1 Why No Conditional Compilation in Tests
**Important Discovery:** CMAB tests do NOT need `#if USE_CMAB` directives!

**Reason:**
- Test project (`OptimizelySDK.Tests.csproj`) targets **.NET 4.5 only**
- Production SDK multi-targets (NET35, NET40, NETSTANDARD1_6, NETSTANDARD2_0)
- In production code: `#if !(NET35 || NET40 || NETSTANDARD1_6)` defines `USE_CMAB`
- Tests always run on NET45 where CMAB is **always available**
- Same pattern as ODP tests (no conditionals in `OdpTests/` files)

**Pattern to Follow:**
```csharp
// ✅ Correct - Like ODP tests
[TestFixture]
public class BucketerCmabTest
{
    [Test]
    public void TestBucketToEntityId()
    {
        // Test CMAB bucketing
    }
}

// ❌ Incorrect - Don't do this
#if USE_CMAB
[TestFixture]
public class BucketerCmabTest { ... }
#endif
```

### 2.3 C# Testing Patterns

#### Mocking Pattern:
```csharp
[SetUp]
public void SetUp()
{
    LoggerMock = new Mock<ILogger>();
    ErrorHandlerMock = new Mock<IErrorHandler>();
    BucketerMock = new Mock<Bucketer>(LoggerMock.Object);
    
    // Setup mock behaviors
    _mockCmabClient = new Mock<ICmabClient>(MockBehavior.Strict);
    _mockCmabClient.Setup(c => c.FetchDecision(...)).Returns("varA");
}
```

#### Test Structure:
```csharp
[Test]
public void TestMethodName()
{
    // Arrange - Setup test data
    var userContext = CreateUserContext(...);
    
    // Act - Execute the method under test
    var result = _service.GetDecision(...);
    
    // Assert - Verify results
    Assert.AreEqual(expected, result);
    _mockCmabClient.Verify(...);
}
```

#### Helper Methods:
```csharp
private ProjectConfig CreateProjectConfig(string ruleId, Experiment experiment, ...)
{
    // Create test configuration
}

private OptimizelyUserContext CreateUserContext(string userId, ...)
{
    // Create test user context
}
```

### 2.4 Existing CMAB Tests in C#
**Currently Exists:**
- ✅ `DefaultCmabServiceTest.cs` - Cache, attribute filtering, options
- ✅ `DefaultCmabClientTest.cs` - HTTP client tests

**Missing (Compared to Swift):**
- ❌ Bucketer tests for `BucketToEntityId()`
- ❌ DecisionService tests for CMAB flow
- ❌ User context decide tests with CMAB
- ❌ Impression event tests with CMAB UUID
- ❌ Integration tests

---

## Phase 3: Comprehensive Test Plan

### 3.1 Test File Structure

```
OptimizelySDK.Tests/
├── BucketerCmabTest.cs                    # NEW - Bucketing CMAB tests
├── DecisionServiceCmabTest.cs             # NEW - Decision service CMAB tests  
├── OptimizelyUserContextCmabTest.cs       # NEW - User context CMAB tests
├── OptimizelyTest.cs                      # MODIFY - Add CMAB impression tests
└── CmabTests/
    ├── DefaultCmabServiceTest.cs          # EXISTS - Enhance coverage
    └── DefaultCmabClientTest.cs           # EXISTS - Already complete
```

### 3.2 Priority Test Coverage

#### **Priority 1: Core Bucketing (BucketerCmabTest.cs)**
**File:** `BucketerCmabTest.cs`
**Focus:** Test the new `BucketToEntityId()` method

Tests to implement:
1. **Test_BucketToEntityId_ReturnsEntityId**
   - Given: Experiment with CMAB config and user
   - When: BucketToEntityId called
   - Then: Returns correct entity ID based on hash

2. **Test_BucketToEntityId_WithFullTrafficAllocation**
   - Given: CMAB experiment with 10000 traffic allocation
   - When: User bucketed
   - Then: User is bucketed into dummy entity

3. **Test_BucketToEntityId_WithZeroTrafficAllocation**
   - Given: CMAB experiment with 0 traffic allocation
   - When: User bucketed
   - Then: Returns null

4. **Test_BucketToEntityId_WithPartialTrafficAllocation**
   - Given: CMAB experiment with 5000 traffic allocation
   - When: Multiple users bucketed
   - Then: Approximately 50% bucketed

5. **Test_BucketToEntityId_MutexGroupAllowed**
   - Given: CMAB experiment in random mutex group
   - When: User bucketed into this experiment
   - Then: Returns entity ID

6. **Test_BucketToEntityId_MutexGroupNotAllowed**
   - Given: CMAB experiment in random mutex group
   - When: User bucketed into different experiment
   - Then: Returns null

7. **Test_BucketToEntityId_HashGeneration**
   - Given: Same user and experiment
   - When: BucketToEntityId called multiple times
   - Then: Returns same entity ID (deterministic)

#### **Priority 2: Decision Service CMAB Flow (DecisionServiceCmabTest.cs)**
**File:** `DecisionServiceCmabTest.cs`
**Focus:** Test `GetDecisionForCmabExperiment()` and `GetVariation()` with CMAB

Tests to implement:
1. **Test_GetVariation_WithCmabExperiment_ReturnsVariation**
   - Given: CMAB experiment, mock CMAB service returns variation ID
   - When: GetVariation called
   - Then: Returns correct variation with CMAB UUID

2. **Test_GetVariation_WithCmabExperiment_ZeroTrafficAllocation**
   - Given: CMAB experiment with 0 traffic
   - When: GetVariation called
   - Then: Returns null, CMAB service not called

3. **Test_GetVariation_WithCmabExperiment_ServiceError**
   - Given: CMAB experiment, CMAB service throws error
   - When: GetVariation called
   - Then: Returns null, error logged in decision reasons

4. **Test_GetVariation_WithCmabExperiment_CacheHit**
   - Given: CMAB decision cached with same attributes
   - When: GetVariation called
   - Then: Returns cached variation, CMAB service not called

5. **Test_GetVariation_WithCmabExperiment_CacheMiss_AttributesChanged**
   - Given: Cached decision exists but attributes changed
   - When: GetVariation called
   - Then: CMAB service called, new decision cached

6. **Test_GetVariationForFeatureExperiment_WithCmab**
   - Given: Feature experiment with CMAB
   - When: GetVariationForFeatureExperiment called
   - Then: Returns FeatureDecision with CMAB UUID

7. **Test_GetVariationForFeature_WithCmabExperiment**
   - Given: Feature flag with CMAB experiment
   - When: GetVariationForFeature called
   - Then: Returns FeatureDecision with correct source and CMAB UUID

8. **Test_GetDecisionForCmabExperiment_AttributeFiltering**
   - Given: User has attributes not in CMAB attributeIds
   - When: GetDecisionForCmabExperiment called
   - Then: Only relevant attributes sent to CMAB service

9. **Test_GetDecisionForCmabExperiment_NoAttributeIds**
   - Given: CMAB experiment with no attributeIds specified
   - When: GetDecisionForCmabExperiment called
   - Then: All user attributes sent to CMAB service

10. **Test_GetVariation_NonCmabExperiment_NotAffected**
    - Given: Regular (non-CMAB) experiment
    - When: GetVariation called
    - Then: Standard bucketing flow, CMAB service not called

#### **Priority 3: User Context Decide Tests (OptimizelyUserContextCmabTest.cs)**
**File:** `OptimizelyUserContextCmabTest.cs`
**Focus:** Integration tests for `Decide()`, `DecideForKeys()`, `DecideAll()` with CMAB

Tests to implement:
1. **Test_Decide_WithCmabExperiment_ReturnsDecision**
   - Given: Feature with CMAB experiment
   - When: user.Decide(flagKey) called
   - Then: Decision has variation, enabled=true, CMAB UUID populated

2. **Test_Decide_WithCmabExperiment_VerifyImpressionEvent**
   - Given: Feature with CMAB experiment
   - When: user.Decide(flagKey) called
   - Then: Impression event sent with CMAB UUID in metadata

3. **Test_Decide_WithCmabExperiment_DisableDecisionEvent**
   - Given: Feature with CMAB experiment, DISABLE_DECISION_EVENT option
   - When: user.Decide(flagKey) called
   - Then: No impression event sent

4. **Test_DecideForKeys_MixedCmabAndNonCmab**
   - Given: Multiple flags, some with CMAB, some without
   - When: user.DecideForKeys([flag1, flag2]) called
   - Then: Correct decisions returned, CMAB service called only for CMAB flags

5. **Test_DecideAll_IncludesCmabExperiments**
   - Given: Project with CMAB and non-CMAB experiments
   - When: user.DecideAll() called
   - Then: All decisions returned with correct CMAB UUIDs

6. **Test_Decide_WithCmabExperiment_IgnoreCmabCache**
   - Given: Feature with CMAB, IGNORE_CMAB_CACHE option
   - When: user.Decide(flagKey) called twice
   - Then: CMAB service called both times

7. **Test_Decide_WithCmabExperiment_ResetCmabCache**
   - Given: Cached CMAB decisions exist, RESET_CMAB_CACHE option
   - When: user.Decide(flagKey) called
   - Then: Entire cache cleared, new decision fetched

8. **Test_Decide_WithCmabExperiment_InvalidateUserCmabCache**
   - Given: Cached CMAB decisions for multiple users, INVALIDATE_USER_CMAB_CACHE option
   - When: user.Decide(flagKey) called
   - Then: Only current user's cache cleared

9. **Test_Decide_WithCmabExperiment_UserProfileService**
   - Given: Feature with CMAB, user profile service enabled
   - When: user.Decide(flagKey) called
   - Then: Variation stored in UPS for subsequent calls

10. **Test_Decide_WithCmabExperiment_IgnoreUserProfileService**
    - Given: Feature with CMAB, IGNORE_USER_PROFILE_SERVICE option
    - When: user.Decide(flagKey) called
    - Then: UPS not consulted, CMAB service called

11. **Test_Decide_WithCmabExperiment_IncludeReasons**
    - Given: Feature with CMAB, INCLUDE_REASONS option
    - When: user.Decide(flagKey) called
    - Then: Decision.Reasons includes CMAB decision info

12. **Test_Decide_WithCmabError_ReturnsErrorDecision**
    - Given: Feature with CMAB, CMAB service errors
    - When: user.Decide(flagKey) called
    - Then: Decision with null variation, error in reasons

13. **Test_Decide_WithCmabExperiment_DecisionNotification**
    - Given: Feature with CMAB, decision notification listener
    - When: user.Decide(flagKey) called
    - Then: Notification fired with CMAB UUID

#### **Priority 4: Impression Event Tests (Modify OptimizelyTest.cs)**
**File:** `OptimizelyTest.cs` (add new tests)
**Focus:** Verify impression events include CMAB UUID

Tests to implement:
1. **Test_SendImpressionEvent_WithCmabUuid**
   - Given: CMAB experiment with UUID
   - When: SendImpressionEvent called
   - Then: Event metadata includes "cmab_uuid" field

2. **Test_SendImpressionEvent_WithoutCmabUuid**
   - Given: Non-CMAB experiment
   - When: SendImpressionEvent called
   - Then: Event metadata does not include "cmab_uuid"

3. **Test_CreateImpressionEvent_CmabUuidInMetadata**
   - Given: UserEventFactory.CreateImpressionEvent with CMAB UUID
   - When: Event created
   - Then: Metadata.CmabUuid populated correctly

4. **Test_EventFactory_CreateLogEvent_WithCmabUuid**
   - Given: UserEvent with CMAB UUID in metadata
   - When: EventFactory.CreateLogEvent called
   - Then: Log event JSON includes "cmab_uuid"

#### **Priority 5: Enhanced CMAB Service Tests (Enhance DefaultCmabServiceTest.cs)**
**File:** `DefaultCmabServiceTest.cs` (add more tests)
**Focus:** Additional edge cases and coverage

Tests to add:
1. **Test_GetDecision_ConcurrentCalls_ThreadSafety**
   - Given: Multiple threads calling GetDecision
   - When: Concurrent calls made
   - Then: No race conditions, correct caching

2. **Test_GetDecision_NullProjectConfig**
   - Given: Null project config
   - When: GetDecision called
   - Then: Appropriate error handling

3. **Test_GetDecision_ExperimentNotFound**
   - Given: Invalid rule ID
   - When: GetDecision called
   - Then: Returns null or error

4. **Test_GetDecision_EmptyAttributeIds**
   - Given: CMAB experiment with empty attributeIds array
   - When: GetDecision called
   - Then: No attributes sent to CMAB service

5. **Test_AttributeFiltering_ComplexAttributes**
   - Given: User has nested objects, arrays in attributes
   - When: Attributes filtered
   - Then: Only simple types included

6. **Test_HashAttributes_LargeAttributeSet**
   - Given: User with 50+ attributes
   - When: HashAttributes called
   - Then: Hash generated efficiently

7. **Test_CacheEviction_LruBehavior**
   - Given: Cache at max size
   - When: New entry added
   - Then: Least recently used entry evicted

---

## Phase 4: Test Data Requirements

### 4.1 Datafile Updates
Need to create/update test datafiles with CMAB experiments:

**File:** `TestData/cmab_datafile.json`
```json
{
  "experiments": [
    {
      "id": "cmab_exp_1",
      "key": "cmab_experiment",
      "status": "Running",
      "layerId": "layer_1",
      "trafficAllocation": [...],
      "audienceIds": [],
      "variations": [
        {"id": "var_a", "key": "a", "featureEnabled": true, ...},
        {"id": "var_b", "key": "b", "featureEnabled": true, ...}
      ],
      "forcedVariations": {},
      "cmab": {
        "trafficAllocation": 10000,
        "attributeIds": ["age_attr", "location_attr"]
      }
    }
  ],
  "featureFlags": [
    {
      "id": "feature_cmab",
      "key": "cmab_feature",
      "experimentIds": ["cmab_exp_1"],
      "rolloutId": "",
      "variables": [...]
    }
  ]
}
```

### 4.2 Mock Data
- Mock CMAB responses (variation IDs, UUIDs)
- Mock attribute sets (age, location, etc.)
- Mock cache states
- Mock error scenarios

---

## Phase 5: Implementation Strategy

### 5.1 Test Development Order
1. **Week 1:** BucketerCmabTest.cs (7 tests)
2. **Week 2:** DecisionServiceCmabTest.cs (10 tests)
3. **Week 3:** OptimizelyUserContextCmabTest.cs (13 tests)
4. **Week 4:** Impression event tests + Enhanced CMAB service tests (11 tests)

**Total:** ~41 new tests

### 5.2 Test Execution Strategy
- Run tests individually during development
- Run full test suite before commit
- **No conditional compilation needed in test files**
  - Test project targets .NET 4.5 where CMAB is always available
  - Follows same pattern as ODP tests (no `#if` directives)

```csharp
[TestFixture]
public class BucketerCmabTest
{
    // Tests here - no #if USE_CMAB needed
}
```

### 5.3 Code Coverage Goals
- **Bucketer.BucketToEntityId()**: 100% coverage
- **DecisionService.GetDecisionForCmabExperiment()**: 100% coverage
- **DefaultCmabService**: 90%+ coverage (already high)
- **Optimizely.SendImpressionEvent()**: CMAB path 100% covered

---

## Phase 6: Test Validation

### 6.1 Test Quality Checklist
For each test:
- [ ] Clear test name describing scenario
- [ ] Follows Arrange-Act-Assert pattern
- [ ] Tests one specific behavior
- [ ] Uses mocks appropriately
- [ ] Verifies all mock interactions
- [ ] Includes positive and negative cases
- [ ] Handles edge cases

### 6.2 Integration Test Validation
- [ ] End-to-end CMAB decision flow works
- [ ] Impression events include CMAB UUID
- [ ] Cache works across multiple decisions
- [ ] Error handling graceful
- [ ] Performance acceptable

---

## Phase 7: Success Criteria

### Test Coverage Metrics
- ✅ All new CMAB methods have unit tests
- ✅ Integration tests cover happy path and error cases
- ✅ Code coverage for CMAB code > 90%
- ✅ All tests pass in CI/CD pipeline
- ✅ Tests written without conditional directives (like ODP tests)

### Functional Validation
- ✅ CMAB experiments work end-to-end
- ✅ Cache behavior correct
- ✅ Events include CMAB UUID
- ✅ Error handling robust
- ✅ Decision reasons populated correctly

---

## Appendix A: Test Utilities

### A.1 Helper Methods Needed
```csharp
// Create CMAB experiment
private Experiment CreateCmabExperiment(string id, int trafficAllocation, List<string> attributeIds);

// Create mock CMAB service
private Mock<ICmabService> CreateMockCmabService(string variationId, string uuid);

// Create user context with attributes
private OptimizelyUserContext CreateUserContext(string userId, Dictionary<string, object> attrs);

// Verify impression event has CMAB UUID
private void AssertImpressionHasCmabUuid(EventForDispatch event, string expectedUuid);
```

### A.2 Test Constants
```csharp
private const string CMAB_EXPERIMENT_ID = "cmab_exp_1";
private const string CMAB_FEATURE_KEY = "cmab_feature";
private const string TEST_USER_ID = "test_user_123";
private const string MOCK_VARIATION_ID = "var_a";
private const string MOCK_CMAB_UUID = "uuid-123-456";
```

---

## Appendix B: Comparison Matrix

| Test Area | Swift SDK | C# SDK Current | C# SDK Needed |
|-----------|-----------|----------------|---------------|
| Bucketer CMAB | 6 tests | 0 tests | ✅ 7 tests |
| Decision Service CMAB | 8 tests | 0 tests | ✅ 10 tests |
| User Context Decide | 13 tests | 0 tests | ✅ 13 tests |
| CMAB Service | 15 tests | 12 tests | ✅ 7 more tests |
| Impression Events | 3 tests | 0 tests | ✅ 4 tests |
| **Total** | **45 tests** | **12 tests** | **+41 tests = 53 total** |

---

## Next Steps

1. **Review this plan** with team
2. **Confirm test priorities** and timeline
3. **Create test data files** (CMAB datafiles)
4. **Begin Phase 1** implementation (BucketerCmabTest.cs)
5. **Iterate and adjust** based on findings

---

**Document Version:** 1.0  
**Last Updated:** October 15, 2025  
**Author:** GitHub Copilot (based on Swift PR #602 and C# SDK analysis)
