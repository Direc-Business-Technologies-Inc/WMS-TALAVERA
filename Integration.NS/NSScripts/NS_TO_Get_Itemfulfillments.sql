SELECT *
FROM (
    SELECT
        t.id AS NetsuiteOrderInternalId,
        t.tranid AS OrderNumber,
        t.recordtype as OrderType,
        t.status AS OrderStatus,

        ABS(tl.quantity) AS QuantityShipped,

        (
            SELECT
                SUM(rtl.quantity)
            FROM
                previoustransactionlinelink pttl
                JOIN transactionline rtl ON pttl.nextline = rtl.id
                AND pttl.nextdoc = rtl.transaction
            WHERE
                pttl.previousdoc = tl.transaction
                AND pttl.previousline = tl.id
                AND pttl.nexttype = 'ItemRcpt'
        ) AS LineQuantityReceived,

	    TO_CHAR(t.createdDate, 'YYYY-MM-DD"T"HH24:MI:SS') AS NetsuiteOrderCreatedDate
    FROM
        transactionline tl
        JOIN transaction t ON tl.transaction = t.id AND tl.mainline = 'T'
    WHERE
        t.recordtype = 'itemfulfillment'
        AND tl.createdfrom = @id
) sub
WHERE
    sub.QuantityShipped != COALESCE(sub.LineQuantityReceived, 0)