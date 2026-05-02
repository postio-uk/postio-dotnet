using System.Text.Json.Serialization;

namespace Postio.Sdk.Models;

/// <summary>Per-request timing breakdown returned with every successful response.</summary>
public sealed record Performance(
    [property: JsonPropertyName("workerMs")] int WorkerMs,
    [property: JsonPropertyName("lookupMs")] int LookupMs);

/// <summary>Response envelope metadata — request_id, count, timing.</summary>
public sealed record Meta(
    [property: JsonPropertyName("countResults")] int CountResults,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("performance")] Performance Performance);

/// <summary>Meta block for /connect (no count field).</summary>
public sealed record MetaConnect(
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("performance")] Performance Performance);

/// <summary>Single typeahead hit from /address/search.</summary>
public sealed record AddressSearchResult(
    [property: JsonPropertyName("udprn")] int Udprn,
    [property: JsonPropertyName("suggestion")] string Suggestion);

/// <summary>Full UK address record from Royal Mail PAF and Ordnance Survey.</summary>
public sealed record Address(
    [property: JsonPropertyName("udprn")] int Udprn,
    [property: JsonPropertyName("postcode")] string Postcode,
    [property: JsonPropertyName("postcode_outward")] string? PostcodeOutward = null,
    [property: JsonPropertyName("postcode_inward")] string? PostcodeInward = null,
    [property: JsonPropertyName("postcode_type")] string? PostcodeType = null,
    [property: JsonPropertyName("address_line_1")] string? AddressLine1 = null,
    [property: JsonPropertyName("address_line_2")] string? AddressLine2 = null,
    [property: JsonPropertyName("address_line_3")] string? AddressLine3 = null,
    [property: JsonPropertyName("post_town")] string? PostTown = null,
    [property: JsonPropertyName("organisation_name")] string? OrganisationName = null,
    [property: JsonPropertyName("department_name")] string? DepartmentName = null,
    [property: JsonPropertyName("building_name")] string? BuildingName = null,
    [property: JsonPropertyName("building_number")] string? BuildingNumber = null,
    [property: JsonPropertyName("sub_building_name")] string? SubBuildingName = null,
    [property: JsonPropertyName("po_box")] string? POBox = null,
    [property: JsonPropertyName("thoroughfare")] string? Thoroughfare = null,
    [property: JsonPropertyName("dependent_thoroughfare")] string? DependentThoroughfare = null,
    [property: JsonPropertyName("dependent_locality")] string? DependentLocality = null,
    [property: JsonPropertyName("double_dependent_locality")] string? DoubleDependentLocality = null,
    [property: JsonPropertyName("delivery_point_suffix")] string? DeliveryPointSuffix = null,
    [property: JsonPropertyName("country")] string? Country = null,
    [property: JsonPropertyName("county")] string? County = null,
    [property: JsonPropertyName("district")] string? District = null,
    [property: JsonPropertyName("ward")] string? Ward = null,
    [property: JsonPropertyName("latitude")] double? Latitude = null,
    [property: JsonPropertyName("longitude")] double? Longitude = null,
    [property: JsonPropertyName("eastings")] int? Eastings = null,
    [property: JsonPropertyName("northings")] int? Northings = null);

/// <summary>Validation verdict for one email address.</summary>
public sealed record EmailResult(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("isValidSyntax")] bool IsValidSyntax,
    [property: JsonPropertyName("didYouMean")] string? DidYouMean,
    [property: JsonPropertyName("isDisposable")] bool IsDisposable,
    [property: JsonPropertyName("isFreeProvider")] bool IsFreeProvider,
    [property: JsonPropertyName("isRoleAccount")] bool IsRoleAccount,
    [property: JsonPropertyName("mxFound")] bool MXFound,
    [property: JsonPropertyName("smtpCheck")] string? SmtpCheck,
    [property: JsonPropertyName("isCatchAll")] bool? IsCatchAll,
    [property: JsonPropertyName("deliverability")] string Deliverability)
{
    public const string DeliverabilityDeliverable   = "deliverable";
    public const string DeliverabilityUndeliverable = "undeliverable";
    public const string DeliverabilityRisky         = "risky";
    public const string DeliverabilityUnknown       = "unknown";
    public const string DeliverabilityInvalid       = "invalid";
}

/// <summary>
/// Validation verdict for one phone number.
///
/// SPEC DRIFT (2026-05-02): the OpenAPI spec marks every nullable field as
/// `required` with type [string, null], but on invalid input the live API
/// drops them entirely. All optional fields default to null here so customer
/// code doesn't see a parse error on real responses. Spec also says
/// IsReachable is string-only; live API returns bool — typed as object to
/// accept either. Reapply this block after any future regen.
/// </summary>
public sealed record PhoneResult(
    [property: JsonPropertyName("number")] string Number,
    [property: JsonPropertyName("isValid")] bool IsValid,
    [property: JsonPropertyName("isPossible")] bool IsPossible,
    [property: JsonPropertyName("type")] string? Type = null,
    [property: JsonPropertyName("countryCode")] string? CountryCode = null,
    [property: JsonPropertyName("countryName")] string? CountryName = null,
    [property: JsonPropertyName("nationalFormat")] string? NationalFormat = null,
    [property: JsonPropertyName("internationalFormat")] string? InternationalFormat = null,
    [property: JsonPropertyName("e164Format")] string? E164Format = null,
    [property: JsonPropertyName("originalCarrier")] string? OriginalCarrier = null,
    [property: JsonPropertyName("currentCarrier")] string? CurrentCarrier = null,
    [property: JsonPropertyName("isPorted")] bool? IsPorted = null,
    [property: JsonPropertyName("isReachable")] object? IsReachable = null,
    [property: JsonPropertyName("mcc")] string? MCC = null,
    [property: JsonPropertyName("mnc")] string? MNC = null,
    [property: JsonPropertyName("level")] string? Level = null,
    [property: JsonPropertyName("lookupError")] string? LookupError = null);

public sealed record AddressSearchEnvelope(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("results")] IReadOnlyList<AddressSearchResult> Results,
    [property: JsonPropertyName("meta")] Meta Meta);

public sealed record AddressPostcodeEnvelope(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("results")] IReadOnlyList<Address> Results,
    [property: JsonPropertyName("meta")] Meta Meta);

public sealed record AddressUdprnEnvelope(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("results")] IReadOnlyList<Address> Results,
    [property: JsonPropertyName("meta")] Meta Meta);

public sealed record EmailEnvelope(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("results")] IReadOnlyList<EmailResult> Results,
    [property: JsonPropertyName("meta")] Meta Meta);

public sealed record PhoneEnvelope(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("results")] IReadOnlyList<PhoneResult> Results,
    [property: JsonPropertyName("meta")] Meta Meta);

public sealed record ConnectSuccess(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("meta")] MetaConnect Meta);

public sealed record ErrorEnvelope(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("error")] string Error,
    [property: JsonPropertyName("details")] string? Details,
    [property: JsonPropertyName("meta")] Meta? Meta);
