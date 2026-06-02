SELECT
	t.id AS NetsuiteOrderInternalId,
	t.tranid AS OrderNumber,
	t.recordtype AS OrderType,
	t.status AS OrderStatus,
	
	tl.location AS LocationInternalId,
	loc.name AS LocationName,
	loc.usebins as LocationUsedBin,
	
	tl.id AS LineSequenceNumber,
	tl.transactionlinetype as TransactionLineType,
	
	t.entity AS VendorEntityId,
	e.fullname AS VendorName,
	ba.custrecord_dbti_vba_assigned_bin AS VendorBinAssignmentId,
	
	i.id AS NetsuiteMaterialInternalId,
	tl.item as MaterialInternalId,
	i.itemid as MaterialCode,
	i.displayname as MaterialName,
	tl.quantity AS LineQuantity,
	tl.units AS UoMId,
	uom.unitname AS UoMName,
	uom.conversionrate AS UoMRate,
	
	TO_CHAR(t.custbody_dbti_est_receipt_date, 'YYYY-MM-DD"T"HH24:MI:SS') AS NetsuiteOrderDocumentDate,
	TO_CHAR(t.createdDate, 'YYYY-MM-DD"T"HH24:MI:SS') AS NetsuiteOrderCreatedDate
FROM
	item i
	JOIN transactionline tl ON i.id = tl.item
	JOIN transaction t ON tl.transaction = t.id
	JOIN entity e ON t.entity = e.id
	JOIN location loc ON tl.location = loc.id
	JOIN customrecord_dbti_vendor_bin_assignment ba ON t.entity = ba.custrecord_dbti_vba_vendor
	JOIN unitstypeuom uom ON tl.units = uom.internalid
	
WHERE
	t.recordtype = 'purchaseorder'
	AND t.status IN ('B', 'E')	AND t.tranid = @tranid
	