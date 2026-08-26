using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Payload;
using System.Text.Json.Serialization;

namespace Application.DataTransferObjects.Transactions.TripTicket.NS.Payload;

public class TripTicketPayloadDTO
{
    [JsonPropertyName("parent")]
    public ReferenceValue? Parent { get; set; }
    [JsonPropertyName("custrecord_dbti_trt_from_subsidiary")]
    public ReferenceValue FromSubsidiary { get; set; }

    [JsonPropertyName("custrecord_dbti_trt_to_subsidiary")]
    public ReferenceValues ToSubsidiary { get; set; }

    [JsonPropertyName("custrecord_dbti_destination")]
    public ReferenceValues Destination { get; set; }

    [JsonPropertyName("custrecord_dbti_trp_truck_plate_no")]
    public ReferenceValue PlateNo { get; set; }

    [JsonPropertyName("custrecord_dbti_trt_assigned_driver")]
    public ReferenceValue Driver { get; set; }

    [JsonPropertyName("custrecord_dbti_trt_date")]
    public string Date { get; set; }

    [JsonPropertyName("custrecord_dbti_trt_truck_seal")]
    public string TruckSeal { get; set; }

    [JsonPropertyName("custrecord_dbti_trt_helper")]
    public ReferenceValue Helper { get; set; }

    [JsonPropertyName("custrecord_dbti_trt_origin_location")]
    public ReferenceValue OriginLocation { get; set; }

    [JsonPropertyName("lines")]
    public List<Itemfulfillments> Lines { get; set; } = new();

    public class Itemfulfillments
    {
        [JsonPropertyName("custrecord_dbti_ttf_item_fulfillment_num")]
        public ReferenceValue ItemFulfillmentNum { get; set; } = new();
    }

    public static TripTicketPayloadDTO CreateTripTicket(
    PostTripTicketDTO tripticket)
    {
        return new TripTicketPayloadDTO
        {
            Parent = tripticket.Parent != 0 ? new ReferenceValue
            {
                Id = tripticket.Parent.ToString()
            } : null,
            FromSubsidiary = new ReferenceValue
            {
                Id = tripticket.FromSubsidiary!.NetsuiteSubsidiaryInternalId.ToString()
            },
            ToSubsidiary = new ReferenceValues
            {
                Ids = tripticket.ToSubsidiaries == null
                    ? new List<string>()
                    : tripticket.ToSubsidiaries
                        .Select(d => d.NetsuiteSubsidiaryInternalId.ToString())
                        .ToList()
            },
            Destination = new ReferenceValues
            {
                Ids = tripticket.Destinations == null
                    ? new List<string>()
                    : tripticket.Destinations
                        .Select(d => d.NetsuiteLocationInternalId.ToString())
                        .ToList()
            },
            PlateNo = new ReferenceValue
            {
                Id = tripticket.TruckPlateNumber!.NetsuiteTruckPlateNoInternalId.ToString()
            },

            Driver = new ReferenceValue
            {
                Id = tripticket.Driver!.NetsuiteEmployeeInternalId.ToString()
            },

            TruckSeal = tripticket.TruckSeal,

            Date = tripticket.TripDate.ToString("MM/dd/yyyy"),

            Helper = new ReferenceValue
            {
                Id = tripticket.Helper!.NetsuiteEmployeeInternalId.ToString()
            },

            OriginLocation = new ReferenceValue
            {
                Id = tripticket.OriginLocation!.NetsuiteLocationInternalId.ToString()
            },

            Lines = tripticket.ItemFulfillments
                .Select(x => new Itemfulfillments
                {
                    ItemFulfillmentNum = new ReferenceValue
                    {
                        Id = x.NetsuiteOrderInternalId.ToString()
                    }
                })
                .ToList() ?? new List<Itemfulfillments>()
        };
    }
}
