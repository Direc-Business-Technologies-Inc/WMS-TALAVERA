using Application.DataTransferObjects.Administration.User;
using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Others.SAP;
using Application.DataTransferObjects.Transactions.Commons;
using Application.DataTransferObjects.Transactions.Delivery;
using Application.DataTransferObjects.Transactions.Delivery.SAP;
using Application.DataTransferObjects.Transactions.Goodsissue;
using Application.DataTransferObjects.Transactions.GoodsIssue;
using Application.DataTransferObjects.Transactions.GoodsReceipt;
using Application.DataTransferObjects.Transactions.GoodsReturn;
using Application.DataTransferObjects.Transactions.GoodsReturn.SAP;
using Application.DataTransferObjects.Transactions.InventoryTransfer;
using Application.DataTransferObjects.Transactions.SalesReturn;
using Application.DataTransferObjects.Transactions.SalesReturn.SAP;
using Application.DataTransferObjects.Transactions.Procurement.Order;
using Application.DataTransferObjects.Transactions.Receiving;
using Domain.Entities.Administration.User.Management;
using Domain.Entities.Enums.Transaction.Commons;
using Domain.Entities.Enums.Transaction.Receiving;
using Domain.ValueObjects.Others;
using Domain.ValueObjects.Transaction;
using Integration.SAP.Entities.Transactional.InventoryTransfer;
using Integration.SAP.Entities.Transactional.Receiving;
using Mapster;
using Shared.Kernel;

namespace Application.UseCases.Registers;

public class MappingRegistration : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        #region DEM to DTO

        #region User Management
        config.NewConfig<UserDEM, UserDataGridDTO>()
            .Map(d => d.Id, s => s.Id)
            .Map(d => d.FullName, s => s.Name.FullName)
            .Map(d => d.Email, s => s.Email.Address)
            .Map(d => d.Phone, s => s.PhoneNumber)
            .Map(d => d.Position, _ => string.Empty)
            .Map(d => d.Active, s => s.Active);
        #endregion User Management

        #endregion DEM to DTO

        #region DTO to VO

        #region System

        #region Common
        config.NewConfig<PersonNameDTO, PersonNameVO>()
            .ConstructUsing(dto => new PersonNameVO(
                dto.FirstName,
                dto.MiddleName,
                dto.LastName));
        #endregion Common

        #region Transactional Documents
        config.NewConfig<AppDocNumDTO, AppDocNumVO>()
            .ConstructUsing(dto => new AppDocNumVO(
                dto.Value));

        config.NewConfig<SapDocumentReferenceDTO, SapDocumentReferenceVO>()
            .ConstructUsing(dto => new SapDocumentReferenceVO(
                dto.DocEntry,
                dto.DocNum,
                dto.BaseEntry,
                dto.BaseDocNum));
        #endregion Transactional Documents

        #endregion System

        #region Administration

        #region User Management

        config.NewConfig<UserNameDTO, UserNameVO>()
            .ConstructUsing(dto => new UserNameVO(dto.Value));
        config.NewConfig<EmailDTO, EmailVO>()
            .ConstructUsing(dto => new EmailVO(dto.Address));
        config.NewConfig<AccountDTO, AccountVO>()
            .ConstructUsing(dto => new AccountVO(
                dto.UserName.Adapt<UserNameVO>(),
                dto.HashedPassword,
                dto.LockoutEnabled,
                dto.Locked));

        #endregion User Management

        #endregion Administration

        #endregion DTO to VO

        #region SAP DTO to DTO

        #region Others

        config.NewConfig<TransactionTypeSAPDTO, TransactionTypeDTO>()
            .Map(d => d.Code, s => s.Code)
            .Map(d => d.Name, s => s.Name)
            .Map(d => d.Account, s => new GLAccountDTO()
            {
                AcctCode = s.AcctCode,
                AcctName = s.AcctName,
            });

        #endregion Others

        #region Delivery

        config.NewConfig<SalesOrderHeaderSAPDTO, SalesOrderDTO>()
            .Map(d => d.SapReference, s => new SapDocumentReferenceDTO()
            {
                DocEntry = s.DocEntry,
                DocNum   = s.DocNum,
            })
            .Map(d => d.BusinessPartner, s => new BusinessPartnerDTO()
            {
                CardCode = s.CardCode,
                CardName = s.CardName,
            })
            .Map(d => d.DocDate,       s => s.DocDate)
            .Map(d => d.DocDueDate,    s => s.DocDueDate)
            .Map(d => d.ContactPerson, s => s.ContactPerson)
            .Map(d => d.SchoolYear,    s => s.SchoolYear)
            .Map(d => d.PONo,          s => s.PONo)
            .Map(d => d.Area,          s => s.Area)
            .Map(d => d.Designation,   s => s.Designation)
            .Map(d => d.OrderedBy,     s => s.OrderBy)       // name mismatch in source
            .Map(d => d.DocRemarks,    s => s.DocRemarks)
            .Map(d => d.PreparedBy,    s => s.PreparedBy)
            .Map(d => d.ReviewedBy,    s => s.ReviewedBy)
            .Map(d => d.ApprovedBy,    s => s.AppprovedBy)   // typo in source
            .Map(d => d.NotedBy,       s => s.NotedBy);

        config.NewConfig<SalesOrderLineSAPDTO, SalesOrderLineDTO>()
            .Map(d => d.LineNum,   s => s.LineNum + 1)       // 0-indexed → 1-indexed
            .Map(d => d.ItemCode,  s => s.ItemCode)
            .Map(d => d.ItemName,  s => s.ItemName)
            .Map(d => d.Quantity,  s => s.Quantity)
            .Map(d => d.UoMCode,   s => s.UoMCode)
            .Map(d => d.UoMValue,  s => s.UoMValue)
            .Map(d => d.UoMName,   s => s.UoMName)
            .Map(d => d.TargetQty, s => s.TargetQty)
            .Map(d => d.OpenQty,   s => s.OpenQty)
            .Map(d => d.Warehouse, s => new WarehouseDTO()
            {
                WhsCode = s.WhsCode,
                WhsName = s.WhsName,
            });

        config.NewConfig<DeliveryHeaderSAPDTO, DeliveryDTO>()
            .Map(d => d.SapReference, s => new SapDocumentReferenceDTO()
            {
                DocEntry = s.DocEntry,
                DocNum   = s.DocNum,
            })
            .Map(d => d.BusinessPartner, s => new BusinessPartnerDTO()
            {
                CardCode = s.CardCode,
                CardName = s.CardName,
            })
            .Map(d => d.DocDate,         s => s.DocDate)
            .Map(d => d.DocDueDate,      s => s.DocDueDate)
            .Map(d => d.NumAtCard,       s => s.NumAtCard)
            .Map(d => d.ContactPerson,   s => s.ContactPerson)
            .Map(d => d.SchoolYear,      s => s.SchoolYear)
            .Map(d => d.DRNo,            s => s.DRNo)
            .Map(d => d.ActualDelivDate, s => s.ActualDeliveryDate)  // name mismatch
            .Map(d => d.DeliveryMeans,   s => s.DeliveryMeans)
            .Map(d => d.Courier,         s => s.Courier)
            .Map(d => d.CourierName,     s => s.CourierName)
            .Map(d => d.Designation,     s => s.Designation)
            .Map(d => d.WayBillNo,       s => s.WaybillNo)           // case mismatch
            .Map(d => d.PlateNo,         s => s.PlateNo)
            .Map(d => d.Driver,          s => s.Driver)
            .Map(d => d.DocRemarks,      s => s.DocRemarks)
            .Map(d => d.ReceivedBy,      s => s.ReceivedBy)
            .Map(d => d.PreparedBy,      s => s.PreparedBy)
            .Map(d => d.ApprovedBy,      s => s.ApprovedBy)
            .Map(d => d.NotedBy,         s => s.NotedBy);

        config.NewConfig<DeliveryLineSAPDTO, DeliveryLineDTO>()
            .Map(d => d.LineNum,   s => s.LineNum + 1)   // 0-indexed → 1-indexed
            .Map(d => d.ItemCode,  s => s.ItemCode)
            .Map(d => d.ItemName,  s => s.ItemName)
            .Map(d => d.Quantity,  s => s.Quantity)
            .Map(d => d.UoMCode,   s => s.UoMCode)
            .Map(d => d.UoMValue,  s => s.UoMValue)
            .Map(d => d.UoMName,   s => s.UoMName)
            .Map(d => d.Warehouse, s => new WarehouseDTO()
            {
                WhsCode = s.WhsCode,
                WhsName = s.WhsName,
            });

        #endregion Delivery

        #region Goods Issue

        config.NewConfig<GoodsIssueHeaderSAPDTO, GoodsIssueDTO>()
            .Map(d => d.SapReference, s => new SapDocumentReferenceDTO()
            {
                DocEntry = s.DocEntry,
                DocNum = s.DocNum,
            })
            .Map(d => d.PreparedBy, s => s.PreparedBy)
            .Map(d => d.TransactionType, s => new TransactionTypeDTO()
            {
                Code = s.TransTypeCode,
                Name = s.TransTypeName,
                Account = new GLAccountDTO()
                {
                    AcctCode = s.AcctCode,
                    AcctName = s.AcctName,
                }
            })
            .Map(d => d.BusinessPartner, s => s.CardCode == null ? null : new BusinessPartnerDTO()
            {
                CardCode = s.CardCode,
                CardName = s.CardName ?? s.CardCode,
            })
            .Map(d => d.DocDate, s => s.DocDate);

        config.NewConfig<GoodsIssueLineSAPDTO, GoodsIssueLineDTO>()
            .Map(d => d.LineNum, s => s.LineNum + 1)
            .Map(d => d.ItemCode, s => s.ItemCode)
            .Map(d => d.ItemName, s => s.ItemName)
            .Map(d => d.Quantity, s => s.Quantity)
            .Map(d => d.UoMCode, s => s.UoMCode)
            .Map(d => d.UoMValue, s => s.UoMValue)
            .Map(d => d.UoMName, s => s.UoMName)
            .Map(d => d.Warehouse, s => new WarehouseDTO()
            {
                WhsCode = s.WhsCode,
                WhsName = s.WhsName,
            });

        #endregion Goods Issue

        #region Goods Receipt

        config.NewConfig<GoodsReceiptHeaderSAPDTO, GoodsReceiptDTO>()
            .Map(d => d.SapReference, s => new SapDocumentReferenceDTO()
            {
                DocEntry = s.DocEntry,
                DocNum = s.DocNum,
            })
            .Map(d => d.PreparedBy, s => s.PreparedBy)
            .Map(d => d.TransactionType, s => new TransactionTypeDTO()
            {
                Code = s.TransTypeCode,
                Name = s.TransTypeName,
                Account = new GLAccountDTO()
                {
                    AcctCode = s.AcctCode,
                    AcctName = s.AcctName,
                }
            })
            .Map(d => d.DocDate, s => s.DocDate);

        config.NewConfig<GoodsReceiptLineSAPDTO, GoodsReceiptLineDTO>()
            .Map(d => d.LineNum, s => s.LineNum + 1)
            .Map(d => d.ItemCode, s => s.ItemCode)
            .Map(d => d.ItemName, s => s.ItemName)
            .Map(d => d.Quantity, s => s.Quantity)
            .Map(d => d.UoMCode, s => s.UoMCode)
            .Map(d => d.UoMValue, s => s.UoMValue)
            .Map(d => d.UoMName, s => s.UoMName)
            .Map(d => d.Warehouse, s => new WarehouseDTO()
            {
                WhsCode = s.WhsCode,
                WhsName = s.WhsName,
            });

        #endregion Goods Receipt

        #region Goods Return

        config.NewConfig<GoodsReturnRequestsSAPDTO, GRRDataGridDTO>()
            .Map(d => d.DocEntry, s => s.DocEntry)
            .Map(d => d.DocNum, s => s.DocNum)
            .Map(d => d.GRPODocEntry, s => s.GRPODocEntry)
            .Map(d => d.GRPODocNum, s => s.GRPODocNum)
            .Map(d => d.PODocEntry, s => s.PODocEntry)
            .Map(d => d.PODocNum, s => s.PODocNum)
            .Map(d => d.DocDate, s => s.DocDate)
            .Map(d => d.DocDueDate, s => s.DocDueDate)
            .Map(d => d.CardCode, s => s.CardCode)
            .Map(d => d.CardName, s => s.CardName)
            .Map(d => d.Remarks, s => s.Remarks);

        config.NewConfig<GRRHeaderSAPDTO, GoodsReturnRequestDTO>()
            .Map(d => d.SapReference, s => new SapDocumentReferenceDTO()
            {
                DocEntry = s.DocEntry,
                DocNum = s.DocNum,
            })
            .Map(d => d.GRPODocEntry, s => s.GRPODocEntry)
            .Map(d => d.GRPODocNum, s => s.GRPODocNum)
            .Map(d => d.PODocEntry, s => s.PODocEntry)
            .Map(d => d.PODocNum, s => s.PODocNum)
            .Map(d => d.DocDueDate, s => s.DocDueDate)
            .Map(d => d.BusinessPartner, s => new BusinessPartnerDTO()
            {
                CardCode = s.CardCode,
                CardName = s.CardName,
            })
            .Map(d => d.Remarks, s => s.Remarks);


        config.NewConfig<GRRLineSAPDTO, GRRLineDTO>()
            .Map(d => d.DocEntry, s => s.DocEntry)
            .Map(d => d.DocNum, s => s.DocNum)
            .Map(d => d.LineNum, s => s.LineNum + 1)
            .Map(d => d.ItemCode, s => s.ItemCode)
            .Map(d => d.ItemName, s => s.ItemName)
            .Map(d => d.TargetQty, s => s.TargetQty)
            .Map(d => d.OpenQty, s => s.OpenQty)
            .Map(d => d.Quantity, s => s.Quantity)
            .Map(d => d.UoMCode, s => s.UoMCode)
            .Map(d => d.UoMValue, s => s.UoMValue)
            .Map(d => d.UoMName, s => s.UoMName)
            .Map(d => d.Warehouse, s => new WarehouseDTO()
            {
                WhsCode = s.WhsCode,
                WhsName = s.WhsName,
            });

        config.NewConfig<GoodsReturnsSAPDTO, GoodsReturnDataGridDTO>()
            .Map(d => d.DocEntry, s => s.DocEntry)
            .Map(d => d.DocNum, s => s.DocNum)
            .Map(d => d.GRRDocEntry, s => s.GRRDocEntry)
            .Map(d => d.GRRDocNum, s => s.GRRDocNum)
            .Map(d => d.GRPODocEntry, s => s.GRPODocEntry)
            .Map(d => d.GRPODocNum, s => s.GRPODocNum)
            .Map(d => d.PODocEntry, s => s.PODocEntry)
            .Map(d => d.PODocNum, s => s.PODocNum)
            .Map(d => d.DocDate, s => s.DocDate)
            .Map(d => d.DocDueDate, s => s.DocDueDate)
            .Map(d => d.CardCode, s => s.CardCode)
            .Map(d => d.CardName, s => s.CardName)
            .Map(d => d.Remarks, s => s.Remarks);

        config.NewConfig<GoodsReturnHeaderSAPDTO, GoodsReturnDTO>()
            .Map(d => d.SapReference, s => new SapDocumentReferenceDTO()
            {
                DocEntry = s.DocEntry,
                DocNum = s.DocNum,
            })
            .Map(d => d.GRPODocEntry, s => s.GRPODocEntry)
            .Map(d => d.GRPODocNum, s => s.GRPODocNum)
            .Map(d => d.PODocEntry, s => s.PODocEntry)
            .Map(d => d.PODocNum, s => s.PODocNum)
            .Map(d => d.DocDueDate, s => s.DocDueDate)
            .Map(d => d.Warehouse, s => new WarehouseDTO()
            {
                WhsCode = s.WhsCode,
                WhsName = s.WhsName,
            })
            .Map(d => d.BusinessPartner, s => new BusinessPartnerDTO()
            {
                CardCode = s.CardCode,
                CardName = s.CardName,
            })
            .Map(d => d.Remarks, s => s.Remarks);

        config.NewConfig<GoodsReturnLineSAPDTO, GoodsReturnLineDTO>()
            .Map(d => d.DocEntry, s => s.DocEntry)
            .Map(d => d.DocNum, s => s.DocNum)
            .Map(d => d.LineNum, s => s.LineNum + 1)
            .Map(d => d.ItemCode, s => s.ItemCode)
            .Map(d => d.ItemName, s => s.ItemName)
            .Map(d => d.Quantity, s => s.Quantity)
            .Map(d => d.UoMCode, s => s.UoMCode)
            .Map(d => d.UoMValue, s => s.UoMValue)
            .Map(d => d.UoMName, s => s.UoMName)
            .Map(d => d.Warehouse, s => new WarehouseDTO()
            {
                WhsCode = s.WhsCode,
                WhsName = s.WhsName,
            });

        config.NewConfig<GoodsReturnRequestDTO, GoodsReturnDTO>()
            .Map(d => d.SapReference, s => new SapDocumentReferenceDTO()
            {
                BaseEntry = s.SapReference.DocEntry,
                BaseDocNum = s.SapReference.DocNum,
            })
            .Map(d => d.BusinessPartner, s => s.BusinessPartner)
            .Map(d => d.DocDate, s => s.DocDate)
            .Map(d => d.DocDueDate, s => s.DocDueDate)
            .Map(d => d.PreparedBy, s => s.PreparedBy);

        config.NewConfig<GRRLineDTO, GoodsReturnLineDTO>()
            .Map(d => d.BaseEntry, s => s.DocEntry)
            .Map(d => d.BaseDocNum, s => s.DocNum)
            .Map(d => d.LineNum, s => s.LineNum)
            .Map(d => d.BaseLine, s => s.LineNum)
            .Map(d => d.ItemCode, s => s.ItemCode)
            .Map(d => d.ItemName, s => s.ItemName)
            .Map(d => d.Quantity, s => s.Quantity)
            .Map(d => d.Warehouse, s => s.Warehouse);


        #endregion Goods Return

        #region Receiving

        config.NewConfig<PurchaseOrderSAPDTO, PurchaseOrderDataGridDTO>()
            .Map(d => d.Id, s => s.DocEntry)
            .Map(d => d.ReferenceNumber, s => s.DocNum)
            .Map(d => d.Date, s => s.DocDate)
            .Map(d => d.DeliveryDate, s => s.DocDueDate)
            .Map(d => d.Status, s => EnumHelper.ParseStringToEnum<DocumentStatus>(s.DocStatus));

        config.NewConfig<PurchaseOrderHeaderSAPDTO, PurchaseOrderDTO>()
            .Map(d => d.SapReference, s => new SapDocumentReferenceDTO()
            {
                DocEntry = s.DocEntry,
                DocNum = s.DocNum,
            })
            .Map(d => d.DocDate, s => s.DocDate)
            .Map(d => d.DocDueDate, s => s.DocDueDate)
            .Map(d => d.BusinessPartner, s => new BusinessPartnerDTO()
            {
                CardCode = s.CardCode,
                CardName = s.CardName,
            })
            .Map(d => d.ItemGroupCodes, s => SplitAndTrim(s.ItemGroupCodes))
            .Map(d => d.SupplierContactPerson, s => s.SupplierContactPerson)
            .Map(d => d.Remarks, s => s.Remarks);

        config.NewConfig<PurchaseOrderLineSAPDTO, PurchaseOrderLineDTO>()
            .Map(d => d.DocEntry, s => s.DocEntry)
            .Map(d => d.DocNum, s => s.DocNum)
            .Map(d => d.LineNum, s => s.LineNum + 1)
            .Map(d => d.ItemCode, s => s.ItemCode)
            .Map(d => d.ItemName, s => s.ItemName)
            .Map(d => d.ISBN, s => s.ISBN)
            .Map(d => d.TargetQuantity, s => s.TargetQuantity)
            .Map(d => d.OpenQuantity, s => s.OpenQuantity)
            .Map(d => d.UoMCode, s => s.UoMCode)
            .Map(d => d.UoMValue, s => s.UoMValue)
            .Map(d => d.UoMName, s => s.UoMName)
            .Map(d => d.Warehouse, s => new WarehouseDTO()
            {
                WhsCode = s.WhsCode,
                WhsName = s.WhsName,
            })
            .Map(d => d.VatGroup, s => s.VatGroup);

        config.NewConfig<PurchaseDeliveryNoteSAPDTO, PurchaseDeliveryNoteDataGridDTO>()
            .Map(d => d.DocEntry, s => s.DocEntry)
            .Map(d => d.DocNum, s => s.DocNum)
            .Map(d => d.BaseEntry, s => s.BaseEntry)
            .Map(d => d.BaseDocNum, s => s.BaseDocNum)
            .Map(d => d.DocDate, s => s.DocDate)
            .Map(d => d.PoDocDate, s => s.PoDocDate)
            .Map(d => d.DocDueDate, s => s.DocDueDate)
            .Map(d => d.CardCode, s => s.CardCode)
            .Map(d => d.CardName, s => s.CardName)
            .Map(d => d.WhsCode, s => s.WhsCode)
            .Map(d => d.WhsName, s => s.WhsName)
            .Map(d => d.ItemDesc, s => s.ItemDesc)
            .Map(d => d.ReceivedBy, s => s.ReceivedBy)
            .Map(d => d.SupplierContactPerson, s => s.SupplierContactPerson);

        config.NewConfig<PurchaseDeliveryNoteHeaderSAPDTO, PurchaseDeliveryNoteDTO>()
           .Map(d => d.SapReference, s => new SapDocumentReferenceDTO()
           {
               DocEntry = s.DocEntry,
               DocNum = s.DocNum,
               BaseEntry = s.BaseEntry,
               BaseDocNum = s.BaseDocNum,
           })
           .Map(d => d.DocDate, s => s.DocDate)
           .Map(d => d.DocDueDate, s => s.DocDueDate)
           .Map(d => d.BusinessPartner, s => new BusinessPartnerDTO()
           {
               CardCode = s.CardCode,
               CardName = s.CardName,
           })
           .Map(d => d.SupplierContactPerson, s => s.SupplierContactPerson)
           .Map(d => d.ReceivedBy, s => s.ReceivedBy);

        config.NewConfig<PurchaseDeliveryNoteLineSAPDTO, PurchaseDeliveryNoteLineDTO>()
            .Map(d => d.DocEntry, s => s.DocEntry)
            .Map(d => d.DocNum, s => s.DocNum)
            .Map(d => d.LineNum, s => s.LineNum + 1)
            .Map(d => d.ItemCode, s => s.ItemCode)
            .Map(d => d.ItemName, s => s.ItemName)
            .Map(d => d.ISBN, s => s.ISBN)
            .Map(d => d.Quantity, s => s.Quantity)
            .Map(d => d.UoMCode, s => s.UoMCode)
            .Map(d => d.UoMValue, s => s.UoMValue)
            .Map(d => d.UoMName, s => s.UoMName)
            .Map(d => d.Warehouse, s => new WarehouseDTO()
            {
                WhsCode = s.WhsCode,
                WhsName = s.WhsName,
            })
            .Map(d => d.InputType, s => EnumHelper.ParseStringToEnum<InputType>(s.InputType));

        config.NewConfig<PurchaseOrderDTO, PurchaseDeliveryNoteDTO>()
            .Map(d => d.SapReference, s => new SapDocumentReferenceDTO()
            {
                BaseEntry = s.SapReference.DocEntry,
                BaseDocNum = s.SapReference.DocNum,
            })
            .Map(d => d.BusinessPartner, s => s.BusinessPartner)
            .Map(d => d.DocDate, s => s.DocDate)
            .Map(d => d.DocDueDate, s => s.DocDueDate)
            .Map(d => d.ReceivedBy, s => s.ReceivedBy);

        config.NewConfig<PurchaseOrderLineDTO, PurchaseDeliveryNoteLineDTO>()
            .Map(d => d.BaseEntry, s => s.DocEntry)
            .Map(d => d.BaseDocNum, s => s.DocNum)
            .Map(d => d.LineNum, s => s.LineNum)
            .Map(d => d.BaseLine, s => s.LineNum)
            .Map(d => d.ItemCode, s => s.ItemCode)
            .Map(d => d.ItemName, s => s.ItemName)
            .Map(d => d.TaxCode, s => s.VatGroup)
            .Map(d => d.Quantity, s => s.Quantity)
            .Map(d => d.Price, s => s.Price)
            .Map(d => d.Warehouse, s => s.Warehouse)
            .Map(d => d.Free, s => s.Free)
            .Map(d => d.InputType, s => s.InputType);

        #endregion Receiving

        #region InventoryTransferRequest
        config.NewConfig<InventoryTransferRequestHeaderSAPDTO, InventoryTransferRequestDTO>()
            .Map(d => d.DocNum, s => s.DocNum)
            .Map(d => d.DocDate, s => s.DocDate)
            .Map(d => d.Remarks, s => s.Remarks)
            .Map(d => d.SchoolYear, s => new SchoolYearDTO()
            {
                Code = s.SchlYearCode,
                Name = s.SchlYearName,
                U_YearFrom = s.U_YearFrom,
                U_YearTo = s.U_YearTo
            })
            .Map(d => d.FromWarehouse, s => new WarehouseDTO()
            {
                WhsCode = s.FrmWhsCode,
                WhsName = s.FrmWhsName
            })
            .Map(d => d.ToWarehouse, s => new WarehouseDTO()
            {
                WhsCode = s.ToWhsCode,
                WhsName = s.ToWhsName
            })
            .Map(d => d.TransferType, s => new TransferTypeDTO()
            {
                Code = s.TransferTypeCode,
                Name = s.TransferTypeName
            })
            .Map(d => d.ApprovedBy, s => s.ApprovedBy)
            .Map(d => d.NotedBy, s => s.NotedBy)
            .Map(d => d.PreparedBy, s => s.PreparedBy);

        config.NewConfig<InventoryTransferHeaderSAPDTO, InventoryTransferDTO>()
            .Map(d => d.DocNum, s => s.DocNum)
            .Map(d => d.DocEntry, s => s.DocEntry)
            .Map(d => d.DocDate, s => s.DocDate)
            .Map(d => d.Remarks, s => s.Remarks)
            .Map(d => d.FromWarehouse, s => new WarehouseDTO()
            {
                WhsCode = s.FrmWhsCode,
                WhsName = s.FrmWhsName
            })
            .Map(d => d.ToWarehouse, s => new WarehouseDTO()
            {
                WhsCode = s.ToWhsCode,
                WhsName = s.ToWhsName
            })
            .Map(d => d.TransferType, s => new TransferTypeDTO()
            {
                Code = s.TransferTypeCode,
                Name = s.TransferTypeName
            }
            )
            .Map(d => d.ApprovedBy, s => s.ApprovedBy)
            .Map(d => d.NotedBy, s => s.NotedBy)
            .Map(d => d.PreparedBy, s => s.PreparedBy);
        #endregion
        #region Sales Return

        config.NewConfig<SalesReturnRequestDTO, SalesReturnDTO>()
            .Map(d => d.SapReference, s => new SapDocumentReferenceDTO()
            {
                BaseEntry = s.SapReference.DocEntry,
                BaseDocNum = s.SapReference.DocNum,
            })
            .Map(d => d.SalesReturnRequestDocEntry, s => s.SapReference.DocEntry)
            .Map(d => d.SalesReturnRequestDocNum, s => s.SapReference.DocNum)
            .Map(d => d.BusinessPartner, s => s.BusinessPartner)
            .Map(d => d.DocDate, s => s.DocDate)
            .Map(d => d.DocDueDate, s => s.DocDueDate)
            .Map(d => d.PreparedBy, s => s.PreparedBy);

        config.NewConfig<SalesReturnRequestLineDTO, SalesReturnLineDTO>()
            .Map(d => d.BaseEntry, s => s.DocEntry)
            .Map(d => d.BaseDocNum, s => s.DocNum)
            .Map(d => d.LineNum, s => s.LineNum)
            .Map(d => d.BaseLine, s => s.LineNum)
            .Map(d => d.ItemCode, s => s.ItemCode)
            .Map(d => d.ItemName, s => s.ItemName)
            .Map(d => d.Quantity, s => s.Quantity)
            .Map(d => d.TargetQuantity, s => s.TargetQuantity)
            .Map(d => d.OpenQuantity, s => s.OpenQuantity)
            .Map(d => d.UoMCode, s => s.UoMCode)
            .Map(d => d.UoMValue, s => s.UoMValue)
            .Map(d => d.UoMName, s => s.UoMName)
            .Map(d => d.Warehouse, s => s.Warehouse);

        config.NewConfig<SalesReturnDataGridSAPDTO, SalesReturnDataGridDTO>();

        config.NewConfig<SalesReturnRequestDataGridSAPDTO, SalesReturnRequestDataGridDTO>();

        config.NewConfig<DataTransferObjects.Transactions.GoodsReturn.SAP.ReturnTypeSAPDTO, DataTransferObjects.Transactions.GoodsReturn.ReturnTypeDTO>();

        config.NewConfig<SalesReturnHeaderSAPDTO, SalesReturnDTO>()
            .Map(d => d.SapReference, s => new SapDocumentReferenceDTO
            {
                DocEntry = s.DocEntry,
                DocNum   = s.DocNum,
            })
            .Map(d => d.BusinessPartner, s => new BusinessPartnerDTO
            {
                CardCode = s.CardCode,
                CardName = s.CardName,
            })
            .Map(d => d.DocDate,       s => s.DocDate)
            .Map(d => d.DocDueDate,    s => s.DocDueDate)
            .Map(d => d.ContactPerson, s => s.ContactPerson)
            .Map(d => d.NumAtCard,     s => s.NumAtCard)
            .Map(d => d.SchoolYear,    s => s.SchoolYear)
            .Map(d => d.ReturnType,    s => s.ReturnType)
            .Map(d => d.PURNo,         s => s.PURNo)
            .Map(d => d.DRNo,          s => s.DRNo)
            .Map(d => d.SONo,          s => s.SONo)
            .Map(d => d.SINo,          s => s.SINo)
            .Map(d => d.Designation,   s => s.Designation)
            .Map(d => d.DocRemarks,    s => s.DocRemarks)
            .Map(d => d.ReturnedBy,    s => s.ReturnedBy)
            .Map(d => d.PickBy,        s => s.PickBy)
            .Map(d => d.PreparedBy,    s => s.PreparedBy)
            .Map(d => d.CheckedBy,     s => s.CheckedBy)
            .Map(d => d.NotedBy,       s => s.NotedBy)
            .Map(d => d.ApprovedBy,    s => s.ApprovedBy);

        config.NewConfig<SalesReturnLinesSAPDTO, SalesReturnLineDTO>()
            .Map(d => d.LineNum,  s => s.LineNum + 1)
            .Map(d => d.DocEntry, s => s.DocEntry)
            .Map(d => d.DocNum,   s => s.DocNum)
            .Map(d => d.ItemCode, s => s.ItemCode)
            .Map(d => d.ItemName, s => s.ItemName)
            .Map(d => d.Quantity, s => s.Quantity)
            .Map(d => d.UoMCode,  s => s.UoMCode)
            .Map(d => d.UoMValue, s => s.UoMValue)
            .Map(d => d.UoMName,  s => s.UoMName);

        config.NewConfig<SalesReturnRequestHeaderSAPDTO, SalesReturnRequestDTO>()
            .Map(d => d.SapReference, s => new SapDocumentReferenceDTO
            {
                DocEntry = s.DocEntry,
                DocNum   = s.DocNum,
            })
            .Map(d => d.BusinessPartner, s => new BusinessPartnerDTO
            {
                CardCode = s.CardCode,
                CardName = s.CardName,
            })
            .Map(d => d.DocDate,       s => s.DocDate)
            .Map(d => d.DocDueDate,    s => s.DocDueDate)
            .Map(d => d.ContactPerson, s => s.ContactPerson)
            .Map(d => d.NumAtCard,     s => s.NumAtCard)
            .Map(d => d.SchoolYear,    s => s.SchoolYear)
            .Map(d => d.ReturnType,    s => s.ReturnType)
            .Map(d => d.PURNo,         s => s.PURNo)
            .Map(d => d.DRNo,          s => s.DRNo)
            .Map(d => d.SONo,          s => s.SONo)
            .Map(d => d.SINo,          s => s.SINo)
            .Map(d => d.Designation,   s => s.Designation)
            .Map(d => d.DocRemarks,    s => s.DocRemarks)
            .Map(d => d.ReturnedBy,    s => s.ReturnedBy)
            .Map(d => d.PickBy,        s => s.PickBy)
            .Map(d => d.PreparedBy,    s => s.PreparedBy)
            .Map(d => d.CheckedBy,     s => s.CheckedBy)
            .Map(d => d.NotedBy,       s => s.NotedBy)
            .Map(d => d.ApprovedBy,    s => s.ApprovedBy);

        config.NewConfig<SalesReturnRequestLinesSAPDTO, SalesReturnRequestLineDTO>()
            .Map(d => d.LineNum,         s => s.LineNum + 1)
            .Map(d => d.DocEntry,        s => s.DocEntry)
            .Map(d => d.DocNum,          s => s.DocNum)
            .Map(d => d.ItemCode,        s => s.ItemCode)
            .Map(d => d.ItemName,        s => s.ItemName)
            .Map(d => d.Quantity,        s => s.Quantity)
            .Map(d => d.UoMCode,         s => s.UoMCode)
            .Map(d => d.UoMValue,        s => s.UoMValue)
            .Map(d => d.UoMName,         s => s.UoMName)
            .Map(d => d.TargetQuantity,  s => s.TargetQuantity)
            .Map(d => d.OpenQuantity,    s => s.OpenQuantity);

        #endregion Sales Return

        #endregion SAP DTO to DTO
    }
    private static List<string> SplitAndTrim(string value)
    {
        return string.IsNullOrEmpty(value)
            ? []
            : [.. value.Split(',').Select(g => g.Trim())];
    }
}



