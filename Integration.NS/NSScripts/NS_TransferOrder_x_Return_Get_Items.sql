SELECT
	t.id AS NetsuiteOrderInternalId,
    t.tranid AS OrderNumber,
    t.recordtype AS OrderType,
    t.status AS OrderStatus,
	t.custbody_dbti_transfer_category as TransferCategory,

    tl.location AS NetsuiteFromLocationInternalId,
    t.transferlocation AS NetsuiteToLocationInternalId,
    t.subsidiary AS NetsuiteFromSubsidiaryInternalId,
    t.tosubsidiary AS NetsuiteToSubsidiaryInternalId,

    loc.name AS LocationName,
    loc.usebins AS IsLocationUsedBin,

    tl.id AS LineSequenceNumber,
    tl.transactionlinetype AS TransactionLineType,

    tl.item AS NetsuiteMaterialInternalId,
    i.itemid AS MaterialCode,
    i.displayname AS MaterialName,

    tl.quantity AS LineQuantity,
	tl.quantityshiprecv AS LineQuantityReceived,

    tl.units AS NetsuiteUoMInternalId,
    uom.unitname AS UoMName,
    uom.conversionrate AS UoMRate,

    TO_CHAR(
        t.createddate,
        'YYYY-MM-DD"T"HH24:MI:SS'
    ) AS NetsuiteOrderCreatedDate

FROM item i
INNER JOIN transactionline tl
    ON i.id = tl.item
INNER JOIN transaction t
    ON tl.transaction = t.id
INNER JOIN location loc
    ON tl.location = loc.id
INNER JOIN unitstypeuom uom
    ON tl.units = uom.internalid

WHERE
    t.recordtype = 'intercompanytransferorder'
    AND t.custbody_dbti_transfer_category IN ('3')
    AND t.status IN ('F', 'E')
	AND tl.transactionlinetype = 'RECEIVING'
	AND t.tranid = @tranid