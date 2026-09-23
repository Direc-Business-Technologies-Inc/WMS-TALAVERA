using Application.DataTransferObjects.Transactions.Commons.NS.Payload;
using System.Text.Json.Serialization;

namespace Application.DataTransferObjects.Transactions.TripTicket.NS.Payload;

public class UpdateTripTicketPayloadDTO
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }
    [JsonPropertyName("parent")]
    public int? Parent { get; set; }
    [JsonPropertyName("custrecord_dbti_trt_from_subsidiary")]
    public int FromSubsidiary { get; set; }

    [JsonPropertyName("custrecord_dbti_trt_to_subsidiary")]
    public List<int> ToSubsidiary { get; set; }

    [JsonPropertyName("custrecord_dbti_destination")]
    public List<int> Destination { get; set; }

    [JsonPropertyName("custrecord_dbti_trp_truck_plate_no")]
    public int PlateNo { get; set; }

    [JsonPropertyName("custrecord_dbti_trt_assigned_driver")]
    public int Driver { get; set; }

    [JsonPropertyName("custrecord_dbti_trt_date")]
    public string Date { get; set; }

    [JsonPropertyName("custrecord_dbti_trt_truck_seal")]
    public string TruckSeal { get; set; }

    [JsonPropertyName("custrecord_dbti_trt_helper")]
    public int Helper { get; set; }

    [JsonPropertyName("custrecord_dbti_trt_origin_location")]
    public int OriginLocation { get; set; }

    [JsonPropertyName("lines")]
    public List<Itemfulfillments> Lines { get; set; } = new();

    public class Itemfulfillments
    {
        [JsonPropertyName("custrecord_dbti_ttf_item_fulfillment_num")]
        public int ItemFulfillmentNum { get; set; } = new();
    }

    [JsonPropertyName("cancelledLines")]
    public List<int> CancelledLines { get; set; } = new();

    public static UpdateTripTicketPayloadDTO UpdateTripTicket(
    PostTripTicketDTO tripticket,
    List<ItemFulfillmentDTO> removedIF,
    List<ItemFulfillmentDTO> addedIF)
    {
        return new UpdateTripTicketPayloadDTO
        {
            Id = tripticket.Id,

            // Extract Internal IDs from object properties/collections
            FromSubsidiary = tripticket.FromSubsidiary?.NetsuiteSubsidiaryInternalId ?? 0,
            ToSubsidiary = tripticket.ToSubsidiaries?.Select(x => x.NetsuiteSubsidiaryInternalId).ToList() ?? new(),
            Destination = tripticket.Destinations?.Select(x => x.NetsuiteLocationInternalId).ToList() ?? new(),

            PlateNo = tripticket.TruckPlateNumber?.NetsuiteTruckPlateNoInternalId ?? 0,
            Driver = tripticket.Driver?.NetsuiteEmployeeInternalId ?? 0,
            Helper = tripticket.Helper?.NetsuiteEmployeeInternalId ?? 0,
            OriginLocation = tripticket.OriginLocation?.NetsuiteLocationInternalId ?? 0,

            Date = tripticket.TripDate.ToString("MM/dd/yyyy") ?? string.Empty,
            TruckSeal = tripticket.TruckSeal ?? string.Empty,

            Lines = addedIF?.Select(x => new Itemfulfillments
            {
                ItemFulfillmentNum = x.NetsuiteOrderInternalId // Uses the internal ID property from the DTO
            }).ToList() ?? new(),

            CancelledLines = removedIF?.Select(x => x.NetsuiteTripTicketLineInternalId).ToList() ?? new()
        };
    }
}
