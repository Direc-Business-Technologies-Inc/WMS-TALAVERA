SELECT
    t.id AS NetsuiteOrderInternalId,
    t.tranid AS OrderNumber,
    t.recordtype AS OrderType,
    t.status AS OrderStatus,

    tl.location AS NetsuiteFromLocationInternalId,
    t.transferlocation AS NetsuiteToLocationInternalId,

    t.subsidiary AS NetsuiteFromSubsidiaryInternalId,
    s.name AS FromSubsidiaryName,

    s.custrecord_dbti_default_bo_location AS NetsuiteSubsidiaryDefaultBOInternalId,
    t.tosubsidiary AS NetsuiteToSubsidiaryInternalId,

    loc.name AS LocationName,
    loc.usebins AS IsLocationUsedBin,
        
    tl.id AS LineSequenceNumber,
    tl.transactionlinetype AS TransactionLineType,

    tl.item AS NetsuiteMaterialInternalId,
    i.itemid AS MaterialCode,
    i.displayname AS MaterialName,
    b.id AS NetsuiteMaterialPrefferedBinId,
	ivb.custrecord_dbti_vba_assigned_bin AS NetsuiteMaterialVendorAssignedBin,
    i.weight AS MaterialWeight,

    ABS(tl.quantity) AS LineQuantity,
    tl.quantitypacked AS LineQuantityPacked,
    tl.quantitybackordered AS LineQuantityBackOrdered,

    tl.units AS NetsuiteUoMInternalId,
    uom.unitname AS UoMName,
    uom.conversionrate AS UoMRate,

    TO_CHAR(t.createddate, 'YYYY-MM-DD"T"HH24:MI:SS') AS NetsuiteOrderCreatedDate

FROM item i
JOIN transactionline tl ON i.id = tl.item
JOIN transaction t ON tl.transaction = t.id
JOIN subsidiary s ON t.subsidiary = s.id
JOIN location loc ON tl.location = loc.id
JOIN unitstypeuom uom ON tl.units = uom.internalid

LEFT JOIN (
    SELECT
        ibq.item,
        ibq.bin,
        b.location
    FROM itembinquantity ibq
    INNER JOIN bin b ON b.id = ibq.bin
    WHERE ibq.preferredbin = 'T'
) ibq ON ibq.item = i.id
   AND ibq.location = tl.location

LEFT JOIN bin b ON b.id = ibq.bin

LEFT JOIN (
    SELECT
        iv.item,
        iv.subsidiary,
        ba.custrecord_dbti_vba_assigned_bin,
        ba.custrecord_dbti_vba_location
    FROM itemvendor iv
    JOIN customrecord_dbti_vendor_bin_assignment ba ON iv.vendor = ba.custrecord_dbti_vba_vendor
    WHERE iv.preferredvendor = 'T'
) ivb ON ivb.item = i.id 
	AND ivb.subsidiary = t.subsidiary 
	AND ivb.custrecord_dbti_vba_location = tl.location

WHERE
    t.recordtype IN ('intercompanytransferorder', 'transferorder')
	AND t.custbody_dbti_transfer_category IN ('1', '2')
    AND t.status IN ('B', 'D', 'E')
    AND tl.transactionlinetype = 'SHIPPING'
	AND t.tranid = @tranid
