CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

ALTER DATABASE CHARACTER SET utf8mb4;

CREATE TABLE `AspNetRoles` (
    `Id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Name` varchar(256) CHARACTER SET utf8mb4 NULL,
    `NormalizedName` varchar(256) CHARACTER SET utf8mb4 NULL,
    `ConcurrencyStamp` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_AspNetRoles` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `AspNetUsers` (
    `Id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Name` longtext CHARACTER SET utf8mb4 NULL,
    `StreetAddress` longtext CHARACTER SET utf8mb4 NULL,
    `City` longtext CHARACTER SET utf8mb4 NULL,
    `State` longtext CHARACTER SET utf8mb4 NULL,
    `PostalCode` longtext CHARACTER SET utf8mb4 NULL,
    `UserName` varchar(256) CHARACTER SET utf8mb4 NULL,
    `NormalizedUserName` varchar(256) CHARACTER SET utf8mb4 NULL,
    `Email` varchar(256) CHARACTER SET utf8mb4 NULL,
    `NormalizedEmail` varchar(256) CHARACTER SET utf8mb4 NULL,
    `EmailConfirmed` tinyint(1) NOT NULL,
    `PasswordHash` longtext CHARACTER SET utf8mb4 NULL,
    `SecurityStamp` longtext CHARACTER SET utf8mb4 NULL,
    `ConcurrencyStamp` longtext CHARACTER SET utf8mb4 NULL,
    `PhoneNumber` longtext CHARACTER SET utf8mb4 NULL,
    `PhoneNumberConfirmed` tinyint(1) NOT NULL,
    `TwoFactorEnabled` tinyint(1) NOT NULL,
    `LockoutEnd` datetime(6) NULL,
    `LockoutEnabled` tinyint(1) NOT NULL,
    `AccessFailedCount` int NOT NULL,
    CONSTRAINT `PK_AspNetUsers` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Categories` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` longtext CHARACTER SET utf8mb4 NOT NULL,
    `DisplayOrder` int NOT NULL,
    CONSTRAINT `PK_Categories` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `AspNetRoleClaims` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `RoleId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ClaimType` longtext CHARACTER SET utf8mb4 NULL,
    `ClaimValue` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_AspNetRoleClaims` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_AspNetRoleClaims_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `AspNetUserClaims` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ClaimType` longtext CHARACTER SET utf8mb4 NULL,
    `ClaimValue` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_AspNetUserClaims` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_AspNetUserClaims_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `AspNetUserLogins` (
    `LoginProvider` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ProviderKey` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ProviderDisplayName` longtext CHARACTER SET utf8mb4 NULL,
    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_AspNetUserLogins` PRIMARY KEY (`LoginProvider`, `ProviderKey`),
    CONSTRAINT `FK_AspNetUserLogins_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `AspNetUserRoles` (
    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `RoleId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_AspNetUserRoles` PRIMARY KEY (`UserId`, `RoleId`),
    CONSTRAINT `FK_AspNetUserRoles_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_AspNetUserRoles_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `AspNetUserTokens` (
    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `LoginProvider` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Value` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_AspNetUserTokens` PRIMARY KEY (`UserId`, `LoginProvider`, `Name`),
    CONSTRAINT `FK_AspNetUserTokens_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `OrderHeaders` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ApplicationUserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `OrderDate` datetime(6) NOT NULL,
    `ShippingDate` datetime(6) NOT NULL,
    `OrderTotal` decimal(18,2) NOT NULL,
    `OrderStatus` longtext CHARACTER SET utf8mb4 NULL,
    `PaymentStatus` longtext CHARACTER SET utf8mb4 NULL,
    `TrackingNumber` longtext CHARACTER SET utf8mb4 NULL,
    `Carrier` longtext CHARACTER SET utf8mb4 NULL,
    `SessionId` longtext CHARACTER SET utf8mb4 NULL,
    `PaymentIntentId` longtext CHARACTER SET utf8mb4 NULL,
    `Name` longtext CHARACTER SET utf8mb4 NULL,
    `StreetAddress` longtext CHARACTER SET utf8mb4 NULL,
    `City` longtext CHARACTER SET utf8mb4 NULL,
    `State` longtext CHARACTER SET utf8mb4 NULL,
    `PostalCode` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_OrderHeaders` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_OrderHeaders_AspNetUsers_ApplicationUserId` FOREIGN KEY (`ApplicationUserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `Products` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Title` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Description` longtext CHARACTER SET utf8mb4 NOT NULL,
    `ISBN` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Author` longtext CHARACTER SET utf8mb4 NOT NULL,
    `ListPrice` decimal(18,2) NOT NULL,
    `Price` decimal(18,2) NOT NULL,
    `Price50` decimal(18,2) NOT NULL,
    `Price100` decimal(18,2) NOT NULL,
    `ImageUrl` longtext CHARACTER SET utf8mb4 NULL,
    `CategoryId` int NOT NULL,
    CONSTRAINT `PK_Products` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Products_Categories_CategoryId` FOREIGN KEY (`CategoryId`) REFERENCES `Categories` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `OrderDetails` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `OrderHeaderId` int NOT NULL,
    `ProductId` int NOT NULL,
    `Count` int NOT NULL,
    `Price` decimal(18,2) NOT NULL,
    CONSTRAINT `PK_OrderDetails` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_OrderDetails_OrderHeaders_OrderHeaderId` FOREIGN KEY (`OrderHeaderId`) REFERENCES `OrderHeaders` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_OrderDetails_Products_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Products` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `ShoppingCarts` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ProductId` int NOT NULL,
    `ApplicationUserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Count` int NOT NULL,
    CONSTRAINT `PK_ShoppingCarts` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ShoppingCarts_AspNetUsers_ApplicationUserId` FOREIGN KEY (`ApplicationUserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ShoppingCarts_Products_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Products` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_AspNetRoleClaims_RoleId` ON `AspNetRoleClaims` (`RoleId`);

CREATE UNIQUE INDEX `RoleNameIndex` ON `AspNetRoles` (`NormalizedName`);

CREATE INDEX `IX_AspNetUserClaims_UserId` ON `AspNetUserClaims` (`UserId`);

CREATE INDEX `IX_AspNetUserLogins_UserId` ON `AspNetUserLogins` (`UserId`);

CREATE INDEX `IX_AspNetUserRoles_RoleId` ON `AspNetUserRoles` (`RoleId`);

CREATE INDEX `EmailIndex` ON `AspNetUsers` (`NormalizedEmail`);

CREATE UNIQUE INDEX `UserNameIndex` ON `AspNetUsers` (`NormalizedUserName`);

CREATE INDEX `IX_OrderDetails_OrderHeaderId` ON `OrderDetails` (`OrderHeaderId`);

CREATE INDEX `IX_OrderDetails_ProductId` ON `OrderDetails` (`ProductId`);

CREATE INDEX `IX_OrderHeaders_ApplicationUserId` ON `OrderHeaders` (`ApplicationUserId`);

CREATE INDEX `IX_Products_CategoryId` ON `Products` (`CategoryId`);

CREATE INDEX `IX_ShoppingCarts_ApplicationUserId` ON `ShoppingCarts` (`ApplicationUserId`);

CREATE INDEX `IX_ShoppingCarts_ProductId` ON `ShoppingCarts` (`ProductId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260215063448_ImproveMoneyAndRelations', '8.0.0');

COMMIT;

START TRANSACTION;

ALTER TABLE `Products` ADD `Stock` int NOT NULL DEFAULT 0;

ALTER TABLE `OrderHeaders` ADD `Email` longtext CHARACTER SET utf8mb4 NULL;

ALTER TABLE `OrderHeaders` ADD `PhoneNumber` longtext CHARACTER SET utf8mb4 NULL;

ALTER TABLE `OrderDetails` ADD `OrderHeaderId1` int NULL;

UPDATE `AspNetUsers` SET `Name` = ''
WHERE `Name` IS NULL;
SELECT ROW_COUNT();


ALTER TABLE `AspNetUsers` MODIFY COLUMN `Name` longtext CHARACTER SET utf8mb4 NOT NULL;

CREATE INDEX `IX_OrderDetails_OrderHeaderId1` ON `OrderDetails` (`OrderHeaderId1`);

ALTER TABLE `OrderDetails` ADD CONSTRAINT `FK_OrderDetails_OrderHeaders_OrderHeaderId1` FOREIGN KEY (`OrderHeaderId1`) REFERENCES `OrderHeaders` (`Id`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260218055032_AddStockToProduct', '8.0.0');

COMMIT;

START TRANSACTION;

ALTER TABLE `OrderHeaders` ADD `OrderStatusValue` int NOT NULL DEFAULT 0;

ALTER TABLE `OrderHeaders` ADD `PaymentStatusValue` int NOT NULL DEFAULT 0;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260218075142_AddStatusEnumsToOrderHeader', '8.0.0');

COMMIT;

START TRANSACTION;

ALTER TABLE `ShoppingCarts` ADD `SelectedOptions` longtext CHARACTER SET utf8mb4 NULL;

ALTER TABLE `ShoppingCarts` ADD `SpecialNotes` longtext CHARACTER SET utf8mb4 NULL;

ALTER TABLE `OrderDetails` ADD `SelectedOptions` longtext CHARACTER SET utf8mb4 NULL;

ALTER TABLE `OrderDetails` ADD `SpecialNotes` longtext CHARACTER SET utf8mb4 NULL;

CREATE TABLE `ProductOptions` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ProductId` int NOT NULL,
    `Name` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Value` longtext CHARACTER SET utf8mb4 NOT NULL,
    `AdditionalPrice` decimal(18,2) NULL,
    `DisplayOrder` int NOT NULL,
    CONSTRAINT `PK_ProductOptions` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ProductOptions_Products_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Products` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_ProductOptions_ProductId` ON `ProductOptions` (`ProductId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260218080524_AddProductOptionsAndNotes', '8.0.0');

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS `POMELO_BEFORE_DROP_PRIMARY_KEY`;
DELIMITER //
CREATE PROCEDURE `POMELO_BEFORE_DROP_PRIMARY_KEY`(IN `SCHEMA_NAME_ARGUMENT` VARCHAR(255), IN `TABLE_NAME_ARGUMENT` VARCHAR(255))
BEGIN
	DECLARE HAS_AUTO_INCREMENT_ID TINYINT(1);
	DECLARE PRIMARY_KEY_COLUMN_NAME VARCHAR(255);
	DECLARE PRIMARY_KEY_TYPE VARCHAR(255);
	DECLARE SQL_EXP VARCHAR(1000);
	SELECT COUNT(*)
		INTO HAS_AUTO_INCREMENT_ID
		FROM `information_schema`.`COLUMNS`
		WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
			AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
			AND `Extra` = 'auto_increment'
			AND `COLUMN_KEY` = 'PRI'
			LIMIT 1;
	IF HAS_AUTO_INCREMENT_ID THEN
		SELECT `COLUMN_TYPE`
			INTO PRIMARY_KEY_TYPE
			FROM `information_schema`.`COLUMNS`
			WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
				AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
				AND `COLUMN_KEY` = 'PRI'
			LIMIT 1;
		SELECT `COLUMN_NAME`
			INTO PRIMARY_KEY_COLUMN_NAME
			FROM `information_schema`.`COLUMNS`
			WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
				AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
				AND `COLUMN_KEY` = 'PRI'
			LIMIT 1;
		SET SQL_EXP = CONCAT('ALTER TABLE `', (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA())), '`.`', TABLE_NAME_ARGUMENT, '` MODIFY COLUMN `', PRIMARY_KEY_COLUMN_NAME, '` ', PRIMARY_KEY_TYPE, ' NOT NULL;');
		SET @SQL_EXP = SQL_EXP;
		PREPARE SQL_EXP_EXECUTE FROM @SQL_EXP;
		EXECUTE SQL_EXP_EXECUTE;
		DEALLOCATE PREPARE SQL_EXP_EXECUTE;
	END IF;
END //
DELIMITER ;

DROP PROCEDURE IF EXISTS `POMELO_AFTER_ADD_PRIMARY_KEY`;
DELIMITER //
CREATE PROCEDURE `POMELO_AFTER_ADD_PRIMARY_KEY`(IN `SCHEMA_NAME_ARGUMENT` VARCHAR(255), IN `TABLE_NAME_ARGUMENT` VARCHAR(255), IN `COLUMN_NAME_ARGUMENT` VARCHAR(255))
BEGIN
	DECLARE HAS_AUTO_INCREMENT_ID INT(11);
	DECLARE PRIMARY_KEY_COLUMN_NAME VARCHAR(255);
	DECLARE PRIMARY_KEY_TYPE VARCHAR(255);
	DECLARE SQL_EXP VARCHAR(1000);
	SELECT COUNT(*)
		INTO HAS_AUTO_INCREMENT_ID
		FROM `information_schema`.`COLUMNS`
		WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
			AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
			AND `COLUMN_NAME` = COLUMN_NAME_ARGUMENT
			AND `COLUMN_TYPE` LIKE '%int%'
			AND `COLUMN_KEY` = 'PRI';
	IF HAS_AUTO_INCREMENT_ID THEN
		SELECT `COLUMN_TYPE`
			INTO PRIMARY_KEY_TYPE
			FROM `information_schema`.`COLUMNS`
			WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
				AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
				AND `COLUMN_NAME` = COLUMN_NAME_ARGUMENT
				AND `COLUMN_TYPE` LIKE '%int%'
				AND `COLUMN_KEY` = 'PRI';
		SELECT `COLUMN_NAME`
			INTO PRIMARY_KEY_COLUMN_NAME
			FROM `information_schema`.`COLUMNS`
			WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
				AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
				AND `COLUMN_NAME` = COLUMN_NAME_ARGUMENT
				AND `COLUMN_TYPE` LIKE '%int%'
				AND `COLUMN_KEY` = 'PRI';
		SET SQL_EXP = CONCAT('ALTER TABLE `', (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA())), '`.`', TABLE_NAME_ARGUMENT, '` MODIFY COLUMN `', PRIMARY_KEY_COLUMN_NAME, '` ', PRIMARY_KEY_TYPE, ' NOT NULL AUTO_INCREMENT;');
		SET @SQL_EXP = SQL_EXP;
		PREPARE SQL_EXP_EXECUTE FROM @SQL_EXP;
		EXECUTE SQL_EXP_EXECUTE;
		DEALLOCATE PREPARE SQL_EXP_EXECUTE;
	END IF;
END //
DELIMITER ;

ALTER TABLE `OrderDetails` DROP FOREIGN KEY `FK_OrderDetails_OrderHeaders_OrderHeaderId`;

ALTER TABLE `OrderDetails` DROP FOREIGN KEY `FK_OrderDetails_OrderHeaders_OrderHeaderId1`;

ALTER TABLE `OrderDetails` DROP FOREIGN KEY `FK_OrderDetails_Products_ProductId`;

CALL POMELO_BEFORE_DROP_PRIMARY_KEY(NULL, 'OrderDetails');
ALTER TABLE `OrderDetails` DROP PRIMARY KEY;

ALTER TABLE `OrderDetails` RENAME `OrderDetail`;

ALTER TABLE `OrderDetail` RENAME INDEX `IX_OrderDetails_ProductId` TO `IX_OrderDetail_ProductId`;

ALTER TABLE `OrderDetail` RENAME INDEX `IX_OrderDetails_OrderHeaderId1` TO `IX_OrderDetail_OrderHeaderId1`;

ALTER TABLE `OrderDetail` RENAME INDEX `IX_OrderDetails_OrderHeaderId` TO `IX_OrderDetail_OrderHeaderId`;

ALTER TABLE `OrderDetail` ADD CONSTRAINT `PK_OrderDetail` PRIMARY KEY (`Id`);
CALL POMELO_AFTER_ADD_PRIMARY_KEY(NULL, 'OrderDetail', 'Id');

CREATE TABLE `Wishlists` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ApplicationUserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ProductId` int NOT NULL,
    `AddedDate` datetime(6) NOT NULL,
    CONSTRAINT `PK_Wishlists` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Wishlists_AspNetUsers_ApplicationUserId` FOREIGN KEY (`ApplicationUserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_Wishlists_Products_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Products` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_Wishlists_ApplicationUserId` ON `Wishlists` (`ApplicationUserId`);

CREATE INDEX `IX_Wishlists_ProductId` ON `Wishlists` (`ProductId`);

ALTER TABLE `OrderDetail` ADD CONSTRAINT `FK_OrderDetail_OrderHeaders_OrderHeaderId` FOREIGN KEY (`OrderHeaderId`) REFERENCES `OrderHeaders` (`Id`) ON DELETE CASCADE;

ALTER TABLE `OrderDetail` ADD CONSTRAINT `FK_OrderDetail_OrderHeaders_OrderHeaderId1` FOREIGN KEY (`OrderHeaderId1`) REFERENCES `OrderHeaders` (`Id`);

ALTER TABLE `OrderDetail` ADD CONSTRAINT `FK_OrderDetail_Products_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Products` (`Id`) ON DELETE RESTRICT;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260218081350_AddWishlistTable', '8.0.0');

DROP PROCEDURE `POMELO_BEFORE_DROP_PRIMARY_KEY`;

DROP PROCEDURE `POMELO_AFTER_ADD_PRIMARY_KEY`;

COMMIT;

START TRANSACTION;

ALTER TABLE `OrderDetail` DROP FOREIGN KEY `FK_OrderDetail_OrderHeaders_OrderHeaderId1`;

ALTER TABLE `OrderDetail` DROP INDEX `IX_OrderDetail_OrderHeaderId1`;

ALTER TABLE `OrderDetail` DROP COLUMN `OrderHeaderId1`;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260218095045_FixOrderDetailRelationship', '8.0.0');

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS `POMELO_BEFORE_DROP_PRIMARY_KEY`;
DELIMITER //
CREATE PROCEDURE `POMELO_BEFORE_DROP_PRIMARY_KEY`(IN `SCHEMA_NAME_ARGUMENT` VARCHAR(255), IN `TABLE_NAME_ARGUMENT` VARCHAR(255))
BEGIN
	DECLARE HAS_AUTO_INCREMENT_ID TINYINT(1);
	DECLARE PRIMARY_KEY_COLUMN_NAME VARCHAR(255);
	DECLARE PRIMARY_KEY_TYPE VARCHAR(255);
	DECLARE SQL_EXP VARCHAR(1000);
	SELECT COUNT(*)
		INTO HAS_AUTO_INCREMENT_ID
		FROM `information_schema`.`COLUMNS`
		WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
			AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
			AND `Extra` = 'auto_increment'
			AND `COLUMN_KEY` = 'PRI'
			LIMIT 1;
	IF HAS_AUTO_INCREMENT_ID THEN
		SELECT `COLUMN_TYPE`
			INTO PRIMARY_KEY_TYPE
			FROM `information_schema`.`COLUMNS`
			WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
				AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
				AND `COLUMN_KEY` = 'PRI'
			LIMIT 1;
		SELECT `COLUMN_NAME`
			INTO PRIMARY_KEY_COLUMN_NAME
			FROM `information_schema`.`COLUMNS`
			WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
				AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
				AND `COLUMN_KEY` = 'PRI'
			LIMIT 1;
		SET SQL_EXP = CONCAT('ALTER TABLE `', (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA())), '`.`', TABLE_NAME_ARGUMENT, '` MODIFY COLUMN `', PRIMARY_KEY_COLUMN_NAME, '` ', PRIMARY_KEY_TYPE, ' NOT NULL;');
		SET @SQL_EXP = SQL_EXP;
		PREPARE SQL_EXP_EXECUTE FROM @SQL_EXP;
		EXECUTE SQL_EXP_EXECUTE;
		DEALLOCATE PREPARE SQL_EXP_EXECUTE;
	END IF;
END //
DELIMITER ;

DROP PROCEDURE IF EXISTS `POMELO_AFTER_ADD_PRIMARY_KEY`;
DELIMITER //
CREATE PROCEDURE `POMELO_AFTER_ADD_PRIMARY_KEY`(IN `SCHEMA_NAME_ARGUMENT` VARCHAR(255), IN `TABLE_NAME_ARGUMENT` VARCHAR(255), IN `COLUMN_NAME_ARGUMENT` VARCHAR(255))
BEGIN
	DECLARE HAS_AUTO_INCREMENT_ID INT(11);
	DECLARE PRIMARY_KEY_COLUMN_NAME VARCHAR(255);
	DECLARE PRIMARY_KEY_TYPE VARCHAR(255);
	DECLARE SQL_EXP VARCHAR(1000);
	SELECT COUNT(*)
		INTO HAS_AUTO_INCREMENT_ID
		FROM `information_schema`.`COLUMNS`
		WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
			AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
			AND `COLUMN_NAME` = COLUMN_NAME_ARGUMENT
			AND `COLUMN_TYPE` LIKE '%int%'
			AND `COLUMN_KEY` = 'PRI';
	IF HAS_AUTO_INCREMENT_ID THEN
		SELECT `COLUMN_TYPE`
			INTO PRIMARY_KEY_TYPE
			FROM `information_schema`.`COLUMNS`
			WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
				AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
				AND `COLUMN_NAME` = COLUMN_NAME_ARGUMENT
				AND `COLUMN_TYPE` LIKE '%int%'
				AND `COLUMN_KEY` = 'PRI';
		SELECT `COLUMN_NAME`
			INTO PRIMARY_KEY_COLUMN_NAME
			FROM `information_schema`.`COLUMNS`
			WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
				AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
				AND `COLUMN_NAME` = COLUMN_NAME_ARGUMENT
				AND `COLUMN_TYPE` LIKE '%int%'
				AND `COLUMN_KEY` = 'PRI';
		SET SQL_EXP = CONCAT('ALTER TABLE `', (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA())), '`.`', TABLE_NAME_ARGUMENT, '` MODIFY COLUMN `', PRIMARY_KEY_COLUMN_NAME, '` ', PRIMARY_KEY_TYPE, ' NOT NULL AUTO_INCREMENT;');
		SET @SQL_EXP = SQL_EXP;
		PREPARE SQL_EXP_EXECUTE FROM @SQL_EXP;
		EXECUTE SQL_EXP_EXECUTE;
		DEALLOCATE PREPARE SQL_EXP_EXECUTE;
	END IF;
END //
DELIMITER ;

ALTER TABLE `OrderDetail` DROP FOREIGN KEY `FK_OrderDetail_OrderHeaders_OrderHeaderId`;

ALTER TABLE `OrderDetail` DROP FOREIGN KEY `FK_OrderDetail_Products_ProductId`;

CALL POMELO_BEFORE_DROP_PRIMARY_KEY(NULL, 'OrderDetail');
ALTER TABLE `OrderDetail` DROP PRIMARY KEY;

ALTER TABLE `OrderDetail` RENAME `OrderDetails`;

ALTER TABLE `OrderDetails` RENAME INDEX `IX_OrderDetail_ProductId` TO `IX_OrderDetails_ProductId`;

ALTER TABLE `OrderDetails` RENAME INDEX `IX_OrderDetail_OrderHeaderId` TO `IX_OrderDetails_OrderHeaderId`;

ALTER TABLE `OrderDetails` ADD CONSTRAINT `PK_OrderDetails` PRIMARY KEY (`Id`);
CALL POMELO_AFTER_ADD_PRIMARY_KEY(NULL, 'OrderDetails', 'Id');

CREATE TABLE `OrderStatusLogs` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `OrderHeaderId` int NOT NULL,
    `FromStatus` longtext CHARACTER SET utf8mb4 NULL,
    `ToStatus` longtext CHARACTER SET utf8mb4 NULL,
    `ChangedAt` datetime(6) NOT NULL,
    `ChangedBy` longtext CHARACTER SET utf8mb4 NULL,
    `Notes` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_OrderStatusLogs` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_OrderStatusLogs_OrderHeaders_OrderHeaderId` FOREIGN KEY (`OrderHeaderId`) REFERENCES `OrderHeaders` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_OrderStatusLogs_OrderHeaderId` ON `OrderStatusLogs` (`OrderHeaderId`);

ALTER TABLE `OrderDetails` ADD CONSTRAINT `FK_OrderDetails_OrderHeaders_OrderHeaderId` FOREIGN KEY (`OrderHeaderId`) REFERENCES `OrderHeaders` (`Id`) ON DELETE CASCADE;

ALTER TABLE `OrderDetails` ADD CONSTRAINT `FK_OrderDetails_Products_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Products` (`Id`) ON DELETE RESTRICT;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260220165243_AddOrderStatusLogs', '8.0.0');

DROP PROCEDURE `POMELO_BEFORE_DROP_PRIMARY_KEY`;

DROP PROCEDURE `POMELO_AFTER_ADD_PRIMARY_KEY`;

COMMIT;

