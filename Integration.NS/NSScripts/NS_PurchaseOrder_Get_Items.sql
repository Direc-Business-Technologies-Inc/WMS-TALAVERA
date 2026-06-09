SELECT
	t.id AS NetsuiteOrderInternalId,
	t.tranid AS OrderNumber,
	t.recordtype AS OrderType,
	t.status AS OrderStatus,
	
	t.subsidiary AS NetsuiteSubsidiaryInternalId,
	s.custrecord_dbti_default_bo_location AS NetsuiteSubsidiaryDefaultBOInternalId,
	
	tl.location AS NetsuiteLocationInternalId,
	loc.name AS LocationName,
	loc.usebins as LocationUsedBin,
	
	tl.id AS LineSequenceNumber,
	tl.transactionlinetype as TransactionLineType,
	
	t.entity AS NetsuiteVendorInternalId,
	e.fullname AS VendorName,
	ba.custrecord_dbti_vba_assigned_bin AS VendorBinAssignmentId,
	
	i.id AS NetsuiteMaterialInternalId,
	tl.item as MaterialInternalId,
	i.itemid as MaterialCode,
	i.displayname as MaterialName,
	b.id AS NetsuiteMaterialPrefferedBinId,
	i.weight AS MaterialWeight,
	tl.quantity AS LineQuantity,
	tl.quantityshiprecv AS LineQuantityReceived,
	tl.units AS NetsuiteUoMInternalId,
	uom.unitname AS UoMName,
	uom.conversionrate AS UoMRate,
	
	TO_CHAR(t.custbody_dbti_est_receipt_date, 'YYYY-MM-DD"T"HH24:MI:SS') AS NetsuiteOrderDocumentDate,
	TO_CHAR(t.createdDate, 'YYYY-MM-DD"T"HH24:MI:SS') AS NetsuiteOrderCreatedDate
FROM item i
    JOIN transactionline tl ON i.id = tl.item
    JOIN transaction t ON tl.transaction = t.id
	JOIN entity e ON t.entity = e.id
    JOIN subsidiary s ON t.subsidiary = s.id
    JOIN location loc ON tl.location = loc.id
    LEFT JOIN (
	   SELECT
		   ibq.item,
		   ibq.bin,
		   b.location
	   FROM itembinquantity ibq
	   JOIN bin b
		   ON b.id = ibq.bin
	   WHERE ibq.preferredbin = 'T'
   ) ibq ON ibq.item = i.id  AND ibq.location = tl.location
	LEFT JOIN bin b ON b.id = ibq.bin
    JOIN unitstypeuom uom ON tl.units = uom.internalid
WHERE
	t.recordtype = 'purchaseorder'
	AND t.status IN ('B', 'E')			
	AND t.tranid = @tranid