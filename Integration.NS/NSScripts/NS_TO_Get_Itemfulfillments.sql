SELECT
    t.id AS NetsuiteOrderInternalId,
    t.tranid AS OrderNumber,
    t.recordtype as OrderType,
    t.status AS OrderStatus,

    TO_CHAR(t.createdDate, 'YYYY-MM-DD"T"HH24:MI:SS') AS NetsuiteOrderCreatedDate
FROM
    transactionline tl
    JOIN transaction t ON tl.transaction = t.id AND tl.mainline = 'T'
WHERE
    t.recordtype = 'itemfulfillment'
    AND t.status = 'C'
    AND NVL(t.custbody_dbti_fully_received, 'F') = 'F'
    AND tl.createdfrom = @id