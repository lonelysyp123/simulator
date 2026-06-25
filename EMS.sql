/*
 Navicat Premium Dump SQL

 Source Server         : 10.37.58.113
 Source Server Type    : MySQL
 Source Server Version : 50735 (5.7.35)
 Source Host           : 10.37.58.113:3306
 Source Schema         : EMS

 Target Server Type    : MySQL
 Target Server Version : 50735 (5.7.35)
 File Encoding         : 65001

 Date: 22/06/2026 17:59:24
*/

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for active
-- ----------------------------
DROP TABLE IF EXISTS `active`;
CREATE TABLE `active` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `active_power` double DEFAULT '0',
  `slope_switch` tinyint(1) DEFAULT '0',
  `slope_control_cycle` int(11) DEFAULT '0',
  `up_slope` double DEFAULT '0',
  `down_slope` double DEFAULT '0',
  `updated_at` int(11) DEFAULT '0',
  PRIMARY KEY (`id`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of active
-- ----------------------------
BEGIN;
INSERT INTO `active` (`id`, `active_power`, `slope_switch`, `slope_control_cycle`, `up_slope`, `down_slope`, `updated_at`) VALUES (1, 0, 1, 6000, 100, 100, 1782111523);
INSERT INTO `active` (`id`, `active_power`, `slope_switch`, `slope_control_cycle`, `up_slope`, `down_slope`, `updated_at`) VALUES (2, 0, 0, 0, 0, 0, 0);
COMMIT;

-- ----------------------------
-- Table structure for bind_mapping
-- ----------------------------
DROP TABLE IF EXISTS `bind_mapping`;
CREATE TABLE `bind_mapping` (
  `meter_id` int(11) DEFAULT NULL,
  `pcs_id` int(11) DEFAULT NULL,
  `pcs_branch_id` int(11) DEFAULT '0',
  `pcs_branch_rate_active_capacity` double DEFAULT '0',
  `pcs_branch_rate_reactive_capacity` double DEFAULT '0',
  `bms_id` int(11) DEFAULT NULL,
  `bms_rate_capacity` double DEFAULT '0',
  `branch_status` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of bind_mapping
-- ----------------------------
BEGIN;
INSERT INTO `bind_mapping` (`meter_id`, `pcs_id`, `pcs_branch_id`, `pcs_branch_rate_active_capacity`, `pcs_branch_rate_reactive_capacity`, `bms_id`, `bms_rate_capacity`, `branch_status`) VALUES (15, 10, 1, 1250, 1250, 0, 5000, 1);
INSERT INTO `bind_mapping` (`meter_id`, `pcs_id`, `pcs_branch_id`, `pcs_branch_rate_active_capacity`, `pcs_branch_rate_reactive_capacity`, `bms_id`, `bms_rate_capacity`, `branch_status`) VALUES (15, 10, 2, 1250, 1250, 1, 5000, 1);
INSERT INTO `bind_mapping` (`meter_id`, `pcs_id`, `pcs_branch_id`, `pcs_branch_rate_active_capacity`, `pcs_branch_rate_reactive_capacity`, `bms_id`, `bms_rate_capacity`, `branch_status`) VALUES (15, 11, 1, 1250, 1250, 2, 5000, 1);
INSERT INTO `bind_mapping` (`meter_id`, `pcs_id`, `pcs_branch_id`, `pcs_branch_rate_active_capacity`, `pcs_branch_rate_reactive_capacity`, `bms_id`, `bms_rate_capacity`, `branch_status`) VALUES (15, 11, 2, 1250, 1250, 3, 5000, 1);
INSERT INTO `bind_mapping` (`meter_id`, `pcs_id`, `pcs_branch_id`, `pcs_branch_rate_active_capacity`, `pcs_branch_rate_reactive_capacity`, `bms_id`, `bms_rate_capacity`, `branch_status`) VALUES (15, 12, 1, 1250, 1250, 4, 5000, 1);
INSERT INTO `bind_mapping` (`meter_id`, `pcs_id`, `pcs_branch_id`, `pcs_branch_rate_active_capacity`, `pcs_branch_rate_reactive_capacity`, `bms_id`, `bms_rate_capacity`, `branch_status`) VALUES (15, 12, 2, 1250, 1250, 5, 5000, 1);
INSERT INTO `bind_mapping` (`meter_id`, `pcs_id`, `pcs_branch_id`, `pcs_branch_rate_active_capacity`, `pcs_branch_rate_reactive_capacity`, `bms_id`, `bms_rate_capacity`, `branch_status`) VALUES (15, 13, 1, 1250, 1250, 8, 5000, 1);
INSERT INTO `bind_mapping` (`meter_id`, `pcs_id`, `pcs_branch_id`, `pcs_branch_rate_active_capacity`, `pcs_branch_rate_reactive_capacity`, `bms_id`, `bms_rate_capacity`, `branch_status`) VALUES (15, 13, 2, 1250, 1250, 9, 5000, 1);
INSERT INTO `bind_mapping` (`meter_id`, `pcs_id`, `pcs_branch_id`, `pcs_branch_rate_active_capacity`, `pcs_branch_rate_reactive_capacity`, `bms_id`, `bms_rate_capacity`, `branch_status`) VALUES (15, 14, 1, 1250, 1250, 6, 5000, 1);
INSERT INTO `bind_mapping` (`meter_id`, `pcs_id`, `pcs_branch_id`, `pcs_branch_rate_active_capacity`, `pcs_branch_rate_reactive_capacity`, `bms_id`, `bms_rate_capacity`, `branch_status`) VALUES (15, 14, 2, 1250, 1250, 7, 5000, 1);
COMMIT;

-- ----------------------------
-- Table structure for bind_strategy_bms
-- ----------------------------
DROP TABLE IF EXISTS `bind_strategy_bms`;
CREATE TABLE `bind_strategy_bms` (
  `id` int(11) NOT NULL,
  `operation_state` varchar(250) NOT NULL,
  `charge_state` varchar(250) NOT NULL,
  `soc` varchar(250) NOT NULL,
  `soh` varchar(250) NOT NULL,
  `max_charge_power` varchar(250) DEFAULT NULL,
  `max_discharge_power` varchar(250) DEFAULT NULL,
  `fault_total` text NOT NULL,
  `start_connection` varchar(250) NOT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of bind_strategy_bms
-- ----------------------------
BEGIN;
INSERT INTO `bind_strategy_bms` (`id`, `operation_state`, `charge_state`, `soc`, `soh`, `max_charge_power`, `max_discharge_power`, `fault_total`, `start_connection`) VALUES (0, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-0-3\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-0-4\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-0-11\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-0-12\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-0-53\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-0-52\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"AggType\": 0,\n    \"CalcMode\": 1,\n    \"SourceCount\": 3,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-0-38\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-0-39\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-0-5\",\n            \"name\": \"\"\n        }\n    ]\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yk-0-165\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}');
INSERT INTO `bind_strategy_bms` (`id`, `operation_state`, `charge_state`, `soc`, `soh`, `max_charge_power`, `max_discharge_power`, `fault_total`, `start_connection`) VALUES (1, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-1-3\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-1-4\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-1-11\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-1-12\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-1-53\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-1-52\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"AggType\": 0,\n    \"CalcMode\": 1,\n    \"SourceCount\": 3,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-1-38\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-1-39\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-1-5\",\n            \"name\": \"\"\n        }\n    ]\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yk-1-165\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}');
INSERT INTO `bind_strategy_bms` (`id`, `operation_state`, `charge_state`, `soc`, `soh`, `max_charge_power`, `max_discharge_power`, `fault_total`, `start_connection`) VALUES (2, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-2-3\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-2-4\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-2-11\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-2-12\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-2-53\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-2-52\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\r\n    \"AggType\": 0,\r\n    \"CalcMode\": 1,\r\n    \"SourceCount\": 3,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-2-38\",\r\n            \"name\": \"\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-2-39\",\r\n            \"name\": \"\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-2-5\",\r\n            \"name\": \"\"\r\n        }\r\n    ]\r\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yk-2-165\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}');
INSERT INTO `bind_strategy_bms` (`id`, `operation_state`, `charge_state`, `soc`, `soh`, `max_charge_power`, `max_discharge_power`, `fault_total`, `start_connection`) VALUES (3, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-3-3\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-3-4\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-3-11\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-3-12\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-3-53\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-3-52\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"AggType\": 0,\n    \"CalcMode\": 1,\n    \"SourceCount\": 3,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-3-38\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-3-39\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-3-5\",\n            \"name\": \"\"\n        }\n    ]\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yk-3-165\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}');
INSERT INTO `bind_strategy_bms` (`id`, `operation_state`, `charge_state`, `soc`, `soh`, `max_charge_power`, `max_discharge_power`, `fault_total`, `start_connection`) VALUES (4, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-4-3\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-4-4\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-4-11\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-4-12\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-4-53\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-4-52\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\r\n    \"AggType\": 0,\r\n    \"CalcMode\": 1,\r\n    \"SourceCount\": 3,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-4-38\",\r\n            \"name\": \"\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-4-39\",\r\n            \"name\": \"\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-4-5\",\r\n            \"name\": \"\"\r\n        }\r\n    ]\r\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yk-4-165\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}');
INSERT INTO `bind_strategy_bms` (`id`, `operation_state`, `charge_state`, `soc`, `soh`, `max_charge_power`, `max_discharge_power`, `fault_total`, `start_connection`) VALUES (5, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-5-3\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-5-4\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-5-11\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-5-12\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-5-53\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-5-52\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\r\n    \"AggType\": 0,\r\n    \"CalcMode\": 1,\r\n    \"SourceCount\": 3,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-5-38\",\r\n            \"name\": \"\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-5-39\",\r\n            \"name\": \"\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-5-5\",\r\n            \"name\": \"\"\r\n        }\r\n    ]\r\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yk-5-165\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}');
INSERT INTO `bind_strategy_bms` (`id`, `operation_state`, `charge_state`, `soc`, `soh`, `max_charge_power`, `max_discharge_power`, `fault_total`, `start_connection`) VALUES (6, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-6-3\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-6-4\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-6-11\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-6-12\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-6-53\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-6-52\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"AggType\": 0,\n    \"CalcMode\": 1,\n    \"SourceCount\": 3,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-6-38\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-6-39\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-6-5\",\n            \"name\": \"\"\n        }\n    ]\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yk-6-165\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}');
INSERT INTO `bind_strategy_bms` (`id`, `operation_state`, `charge_state`, `soc`, `soh`, `max_charge_power`, `max_discharge_power`, `fault_total`, `start_connection`) VALUES (7, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-7-3\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-7-4\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-7-11\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-7-12\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-7-53\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-7-52\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"AggType\": 0,\n    \"CalcMode\": 1,\n    \"SourceCount\": 3,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-7-38\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-7-39\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-7-5\",\n            \"name\": \"\"\n        }\n    ]\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yk-7-165\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}');
INSERT INTO `bind_strategy_bms` (`id`, `operation_state`, `charge_state`, `soc`, `soh`, `max_charge_power`, `max_discharge_power`, `fault_total`, `start_connection`) VALUES (8, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-8-3\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-8-4\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-8-11\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-8-12\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-8-53\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-8-52\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"AggType\": 0,\n    \"CalcMode\": 1,\n    \"SourceCount\": 3,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-8-38\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-8-39\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-8-5\",\n            \"name\": \"\"\n        }\n    ]\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yk-8-165\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}');
INSERT INTO `bind_strategy_bms` (`id`, `operation_state`, `charge_state`, `soc`, `soh`, `max_charge_power`, `max_discharge_power`, `fault_total`, `start_connection`) VALUES (9, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-9-3\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-9-4\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-9-11\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-9-12\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-9-53\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-9-52\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"AggType\": 0,\n    \"CalcMode\": 1,\n    \"SourceCount\": 3,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-9-38\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-9-39\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-9-5\",\n            \"name\": \"\"\n        }\n    ]\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yk-9-165\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}');
COMMIT;

-- ----------------------------
-- Table structure for bind_strategy_meter
-- ----------------------------
DROP TABLE IF EXISTS `bind_strategy_meter`;
CREATE TABLE `bind_strategy_meter` (
  `id` int(11) NOT NULL,
  `Uab` varchar(250) NOT NULL,
  `Ubc` varchar(250) NOT NULL,
  `Uca` varchar(250) NOT NULL,
  `activepower` varchar(250) NOT NULL,
  `reactivepower` varchar(250) NOT NULL,
  `apparentpower` varchar(250) NOT NULL,
  `powerfactor` varchar(250) NOT NULL,
  `frequency` varchar(250) NOT NULL,
  `fault_total` text NOT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of bind_strategy_meter
-- ----------------------------
BEGIN;
INSERT INTO `bind_strategy_meter` (`id`, `Uab`, `Ubc`, `Uca`, `activepower`, `reactivepower`, `apparentpower`, `powerfactor`, `frequency`, `fault_total`) VALUES (15, '{\n    \"CalcMode\": 2,\n    \"Key\": \"\",\n    \"MergeInfo\": {\n        \"Coeff\": 0.001,\n        \"DataType\": 3,\n        \"HighPoint\": \"yc-15-6\",\n        \"LowPoint\": \"yc-15-7\"\n    }\n}', '{\n    \"CalcMode\": 2,\n    \"Key\": \"\",\n    \"MergeInfo\": {\n        \"Coeff\": 0.001,\n        \"DataType\": 3,\n        \"HighPoint\": \"yc-15-8\",\n        \"LowPoint\": \"yc-15-9\"\n    }\n}', '{\n    \"CalcMode\": 2,\n    \"Key\": \"\",\n    \"MergeInfo\": {\n        \"Coeff\": 0.001,\n        \"DataType\": 3,\n        \"HighPoint\": \"yc-15-10\",\n        \"LowPoint\": \"yc-15-11\"\n    }\n}', '{\n    \"CalcMode\": 2,\n    \"Key\": \"\",\n    \"MergeInfo\": {\n        \"Coeff\": 0.001,\n        \"DataType\": 3,\n        \"HighPoint\": \"yc-15-24\",\n        \"LowPoint\": \"yc-15-25\"\n    }\n}', '{\n    \"CalcMode\": 2,\n    \"Key\": \"\",\n    \"MergeInfo\": {\n        \"Coeff\": 0.001,\n        \"DataType\": 3,\n        \"HighPoint\": \"yc-15-32\",\n        \"LowPoint\": \"yc-15-33\"\n    }\n}', '{\n    \"CalcMode\": 2,\n    \"Key\": \"\",\n    \"MergeInfo\": {\n        \"Coeff\": 0.001,\n        \"DataType\": 2,\n        \"HighPoint\": \"yc-15-34\",\n        \"LowPoint\": \"yc-15-35\"\n    }\n}', '{\n    \"CalcMode\": 2,\n    \"Key\": \"\",\n    \"MergeInfo\": {\n        \"Coeff\": 0.001,\n        \"DataType\": 3,\n        \"HighPoint\": \"yc-15-36\",\n        \"LowPoint\": \"yc-15-37\"\n    }\n}', '{\n    \"CalcMode\": 2,\n    \"Key\": \"\",\n    \"MergeInfo\": {\n        \"Coeff\": 0.001,\n        \"DataType\": 3,\n        \"HighPoint\": \"yc-15-38\",\n        \"LowPoint\": \"yc-15-39\"\n    }\n}', '');
COMMIT;

-- ----------------------------
-- Table structure for bind_strategy_pcs
-- ----------------------------
DROP TABLE IF EXISTS `bind_strategy_pcs`;
CREATE TABLE `bind_strategy_pcs` (
  `id` int(11) NOT NULL,
  `high_voltage_command` varchar(250) NOT NULL,
  `low_voltage_command` varchar(250) NOT NULL,
  `fault_total` text NOT NULL,
  `warning_total` text NOT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of bind_strategy_pcs
-- ----------------------------
BEGIN;
INSERT INTO `bind_strategy_pcs` (`id`, `high_voltage_command`, `low_voltage_command`, `fault_total`, `warning_total`) VALUES (10, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yk-10-101\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yk-10-101\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\r\n    \"AggType\": 0,\r\n    \"CalcMode\": 1,\r\n    \"Key\": \"yctotal-10-2\",\r\n    \"SourceCount\": 10,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-10-10\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-10-11\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-10-12\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-10-13\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-10-14\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-10-15\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-10-16\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-10-17\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-10-18\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-10-19\"\r\n        }\r\n    ]\r\n}', '{\n    \"AggType\": 0,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 10,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-10-0\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-10-1\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-10-2\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-10-3\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-10-4\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-10-5\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-10-6\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-10-7\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-10-8\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-10-9\",\n            \"name\": \"\"\n        }\n    ]\n}');
INSERT INTO `bind_strategy_pcs` (`id`, `high_voltage_command`, `low_voltage_command`, `fault_total`, `warning_total`) VALUES (11, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yk-11-101\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yk-11-101\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\r\n    \"AggType\": 0,\r\n    \"CalcMode\": 1,\r\n    \"Key\": \"yctotal-11-2\",\r\n    \"SourceCount\": 10,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-11-10\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-11-11\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-11-12\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-11-13\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-11-14\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-11-15\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-11-16\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-11-17\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-11-18\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-11-19\"\r\n        }\r\n    ]\r\n}', '{\n    \"AggType\": 0,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 10,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-11-0\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-11-1\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-11-2\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-11-3\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-11-4\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-11-5\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-11-6\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-11-7\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-11-8\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-11-9\",\n            \"name\": \"\"\n        }\n    ]\n}');
INSERT INTO `bind_strategy_pcs` (`id`, `high_voltage_command`, `low_voltage_command`, `fault_total`, `warning_total`) VALUES (12, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yk-12-101\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yk-12-101\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"AggType\": 0,\n    \"CalcMode\": 1,\n    \"SourceCount\": 10,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-10\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-11\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-12\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-13\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-14\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-15\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-16\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-17\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-18\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-19\",\n            \"name\": \"\"\n        }\n    ]\n}', '{\n    \"AggType\": 0,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 10,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-0\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-1\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-2\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-3\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-4\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-5\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-6\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-7\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-8\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-9\",\n            \"name\": \"\"\n        }\n    ]\n}');
INSERT INTO `bind_strategy_pcs` (`id`, `high_voltage_command`, `low_voltage_command`, `fault_total`, `warning_total`) VALUES (13, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yk-13-101\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yk-13-101\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\r\n    \"AggType\": 0,\r\n    \"CalcMode\": 1,\r\n    \"Key\": \"yctotal-13-2\",\r\n    \"SourceCount\": 10,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-13-10\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-13-11\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-13-12\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-13-13\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-13-14\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-13-15\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-13-16\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-13-17\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-13-18\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-13-19\"\r\n        }\r\n    ]\r\n}', '{\n    \"AggType\": 0,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 10,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-13-0\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-13-1\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-13-2\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-13-3\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-13-4\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-13-5\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-13-6\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-13-7\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-13-8\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-13-9\",\n            \"name\": \"\"\n        }\n    ]\n}');
INSERT INTO `bind_strategy_pcs` (`id`, `high_voltage_command`, `low_voltage_command`, `fault_total`, `warning_total`) VALUES (14, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yk-14-101\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yk-14-101\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"AggType\": 0,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 10,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-10\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-11\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-12\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-13\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-14\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-15\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-16\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-17\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-18\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-19\",\n            \"name\": \"\"\n        }\n    ]\n}', '{\n    \"AggType\": 0,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 10,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-0\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-1\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-2\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-3\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-4\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-5\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-6\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-7\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-8\",\n            \"name\": \"\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-9\",\n            \"name\": \"\"\n        }\n    ]\n}');
COMMIT;

-- ----------------------------
-- Table structure for bind_strategy_pcsbranch
-- ----------------------------
DROP TABLE IF EXISTS `bind_strategy_pcsbranch`;
CREATE TABLE `bind_strategy_pcsbranch` (
  `id` int(11) NOT NULL,
  `branch_id` int(11) NOT NULL,
  `ac_active_power` varchar(250) NOT NULL,
  `ac_reactive_power` varchar(250) NOT NULL,
  `operation_status` varchar(250) NOT NULL,
  `on_off_command` varchar(250) NOT NULL,
  `active_power_set` varchar(250) NOT NULL,
  `reactive_power_set` varchar(250) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of bind_strategy_pcsbranch
-- ----------------------------
BEGIN;
INSERT INTO `bind_strategy_pcsbranch` (`id`, `branch_id`, `ac_active_power`, `ac_reactive_power`, `operation_status`, `on_off_command`, `active_power_set`, `reactive_power_set`) VALUES (10, 1, '{\n    \"CalcMode\": 2,\n    \"Key\": \"\",\n    \"MergeInfo\": {\n        \"Coeff\": 0.1,\n        \"DataType\": 3,\n        \"HighPoint\": \"yc-10-28\",\n        \"LowPoint\": \"yc-10-29\"\n    }\n}', '{\n    \"CalcMode\": 2,\n    \"Key\": \"\",\n    \"MergeInfo\": {\n        \"Coeff\": 0.1,\n        \"DataType\": 3,\n        \"HighPoint\": \"yc-10-30\",\n        \"LowPoint\": \"yc-10-31\"\n    }\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-10-53\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yk-10-96\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yk-10-93\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yk-10-94\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}');
INSERT INTO `bind_strategy_pcsbranch` (`id`, `branch_id`, `ac_active_power`, `ac_reactive_power`, `operation_status`, `on_off_command`, `active_power_set`, `reactive_power_set`) VALUES (10, 2, '{\n    \"CalcMode\": 2,\n    \"Key\": \"\",\n    \"MergeInfo\": {\n        \"Coeff\": 0.1,\n        \"DataType\": 3,\n        \"HighPoint\": \"yc-10-64\",\n        \"LowPoint\": \"yc-10-65\"\n    }\n}', '{\n    \"CalcMode\": 2,\n    \"Key\": \"\",\n    \"MergeInfo\": {\n        \"Coeff\": 0.1,\n        \"DataType\": 3,\n        \"HighPoint\": \"yc-10-66\",\n        \"LowPoint\": \"yc-10-67\"\n    }\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-10-89\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yk-10-100\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yk-10-97\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yk-10-98\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}');
INSERT INTO `bind_strategy_pcsbranch` (`id`, `branch_id`, `ac_active_power`, `ac_reactive_power`, `operation_status`, `on_off_command`, `active_power_set`, `reactive_power_set`) VALUES (11, 1, '{\n    \"CalcMode\": 2,\n    \"Key\": \"\",\n    \"MergeInfo\": {\n        \"Coeff\": 0.1,\n        \"DataType\": 3,\n        \"HighPoint\": \"yc-11-28\",\n        \"LowPoint\": \"yc-11-29\"\n    }\n}', '{\n    \"CalcMode\": 2,\n    \"Key\": \"\",\n    \"MergeInfo\": {\n        \"Coeff\": 0.1,\n        \"DataType\": 3,\n        \"HighPoint\": \"yc-11-30\",\n        \"LowPoint\": \"yc-11-31\"\n    }\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-11-53\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yk-11-96\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yk-11-93\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yk-11-94\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}');
INSERT INTO `bind_strategy_pcsbranch` (`id`, `branch_id`, `ac_active_power`, `ac_reactive_power`, `operation_status`, `on_off_command`, `active_power_set`, `reactive_power_set`) VALUES (11, 2, '{\n    \"CalcMode\": 2,\n    \"Key\": \"\",\n    \"MergeInfo\": {\n        \"Coeff\": 0.1,\n        \"DataType\": 3,\n        \"HighPoint\": \"yc-11-64\",\n        \"LowPoint\": \"yc-11-65\"\n    }\n}', '{\n    \"CalcMode\": 2,\n    \"Key\": \"\",\n    \"MergeInfo\": {\n        \"Coeff\": 0.1,\n        \"DataType\": 3,\n        \"HighPoint\": \"yc-11-66\",\n        \"LowPoint\": \"yc-11-67\"\n    }\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-11-89\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yk-11-100\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yk-11-97\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yk-11-98\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}');
INSERT INTO `bind_strategy_pcsbranch` (`id`, `branch_id`, `ac_active_power`, `ac_reactive_power`, `operation_status`, `on_off_command`, `active_power_set`, `reactive_power_set`) VALUES (12, 1, '{\n    \"CalcMode\": 2,\n    \"Key\": \"\",\n    \"MergeInfo\": {\n        \"Coeff\": 0.1,\n        \"DataType\": 3,\n        \"HighPoint\": \"yc-12-28\",\n        \"LowPoint\": \"yc-12-29\"\n    }\n}', '{\n    \"CalcMode\": 2,\n    \"Key\": \"\",\n    \"MergeInfo\": {\n        \"Coeff\": 0.1,\n        \"DataType\": 3,\n        \"HighPoint\": \"yc-12-30\",\n        \"LowPoint\": \"yc-12-31\"\n    }\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-12-53\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yk-12-96\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yk-12-93\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yk-12-94\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}');
INSERT INTO `bind_strategy_pcsbranch` (`id`, `branch_id`, `ac_active_power`, `ac_reactive_power`, `operation_status`, `on_off_command`, `active_power_set`, `reactive_power_set`) VALUES (12, 2, '{\n    \"CalcMode\": 2,\n    \"Key\": \"\",\n    \"MergeInfo\": {\n        \"Coeff\": 0.1,\n        \"DataType\": 3,\n        \"HighPoint\": \"yc-12-64\",\n        \"LowPoint\": \"yc-12-65\"\n    }\n}', '{\n    \"CalcMode\": 2,\n    \"Key\": \"\",\n    \"MergeInfo\": {\n        \"Coeff\": 0.1,\n        \"DataType\": 3,\n        \"HighPoint\": \"yc-12-66\",\n        \"LowPoint\": \"yc-12-67\"\n    }\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-12-89\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yk-12-100\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yk-12-97\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yk-12-98\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}');
INSERT INTO `bind_strategy_pcsbranch` (`id`, `branch_id`, `ac_active_power`, `ac_reactive_power`, `operation_status`, `on_off_command`, `active_power_set`, `reactive_power_set`) VALUES (13, 1, '{\n    \"CalcMode\": 2,\n    \"Key\": \"\",\n    \"MergeInfo\": {\n        \"Coeff\": 0.1,\n        \"DataType\": 3,\n        \"HighPoint\": \"yc-13-28\",\n        \"LowPoint\": \"yc-13-29\"\n    }\n}', '{\n    \"CalcMode\": 2,\n    \"Key\": \"\",\n    \"MergeInfo\": {\n        \"Coeff\": 0.1,\n        \"DataType\": 3,\n        \"HighPoint\": \"yc-13-30\",\n        \"LowPoint\": \"yc-13-31\"\n    }\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-13-53\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yk-13-96\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yk-13-93\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yk-13-94\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}');
INSERT INTO `bind_strategy_pcsbranch` (`id`, `branch_id`, `ac_active_power`, `ac_reactive_power`, `operation_status`, `on_off_command`, `active_power_set`, `reactive_power_set`) VALUES (13, 2, '{\n    \"CalcMode\": 2,\n    \"Key\": \"\",\n    \"MergeInfo\": {\n        \"Coeff\": 0.1,\n        \"DataType\": 3,\n        \"HighPoint\": \"yc-13-64\",\n        \"LowPoint\": \"yc-13-65\"\n    }\n}', '{\n    \"CalcMode\": 2,\n    \"Key\": \"\",\n    \"MergeInfo\": {\n        \"Coeff\": 0.1,\n        \"DataType\": 3,\n        \"HighPoint\": \"yc-13-66\",\n        \"LowPoint\": \"yc-13-67\"\n    }\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-13-89\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yk-13-100\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yk-13-97\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yk-13-98\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}');
INSERT INTO `bind_strategy_pcsbranch` (`id`, `branch_id`, `ac_active_power`, `ac_reactive_power`, `operation_status`, `on_off_command`, `active_power_set`, `reactive_power_set`) VALUES (14, 1, '{\n    \"CalcMode\": 2,\n    \"Key\": \"\",\n    \"MergeInfo\": {\n        \"Coeff\": 0.1,\n        \"DataType\": 3,\n        \"HighPoint\": \"yc-14-28\",\n        \"LowPoint\": \"yc-14-29\"\n    }\n}', '{\n    \"CalcMode\": 2,\n    \"Key\": \"\",\n    \"MergeInfo\": {\n        \"Coeff\": 0.1,\n        \"DataType\": 3,\n        \"HighPoint\": \"yc-14-30\",\n        \"LowPoint\": \"yc-14-31\"\n    }\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-14-53\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yk-14-96\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yk-14-93\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yk-14-94\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}');
INSERT INTO `bind_strategy_pcsbranch` (`id`, `branch_id`, `ac_active_power`, `ac_reactive_power`, `operation_status`, `on_off_command`, `active_power_set`, `reactive_power_set`) VALUES (14, 2, '{\n    \"CalcMode\": 2,\n    \"Key\": \"\",\n    \"MergeInfo\": {\n        \"Coeff\": 0.1,\n        \"DataType\": 3,\n        \"HighPoint\": \"yc-14-64\",\n        \"LowPoint\": \"yc-14-65\"\n    }\n}', '{\n    \"CalcMode\": 2,\n    \"Key\": \"\",\n    \"MergeInfo\": {\n        \"Coeff\": 0.1,\n        \"DataType\": 3,\n        \"HighPoint\": \"yc-14-66\",\n        \"LowPoint\": \"yc-14-67\"\n    }\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-14-89\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yk-14-100\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yk-14-97\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yk-14-98\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}');
COMMIT;

-- ----------------------------
-- Table structure for bind_web_bms
-- ----------------------------
DROP TABLE IF EXISTS `bind_web_bms`;
CREATE TABLE `bind_web_bms` (
  `id` int(11) NOT NULL,
  `total_voltage` varchar(250) NOT NULL,
  `total_current` varchar(250) NOT NULL,
  `online_rack_number` varchar(250) NOT NULL,
  `fault` text NOT NULL,
  `alarm` text NOT NULL,
  `warning` text NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of bind_web_bms
-- ----------------------------
BEGIN;
INSERT INTO `bind_web_bms` (`id`, `total_voltage`, `total_current`, `online_rack_number`, `fault`, `alarm`, `warning`) VALUES (0, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-0-9\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-0-10\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-0-47\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"AggType\": 3,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 2,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-0-5\",\n            \"name\": \"总控报警状态1\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-0-8\",\n            \"name\": \"系统一级报警汇总\"\n        }\n    ]\n}', '{\n    \"AggType\": 3,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 1,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-0-7\",\n            \"name\": \"系统二级报警汇总\"\n        }\n    ]\n}', '{\n    \"AggType\": 3,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 1,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-0-6\",\n            \"name\": \"系统三级报警汇总\"\n        }\n    ]\n}');
INSERT INTO `bind_web_bms` (`id`, `total_voltage`, `total_current`, `online_rack_number`, `fault`, `alarm`, `warning`) VALUES (1, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-1-9\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-1-10\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-1-47\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\r\n    \"AggType\": 3,\r\n    \"CalcMode\": 1,\r\n    \"Key\": \"\",\r\n    \"SourceCount\": 2,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-1-5\",\r\n            \"name\": \"总控报警状态1\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-1-8\",\r\n            \"name\": \"系统一级报警汇总\"\r\n        }\r\n    ]\r\n}', '{\r\n    \"AggType\": 3,\r\n    \"CalcMode\": 1,\r\n    \"Key\": \"\",\r\n    \"SourceCount\": 1,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-1-7\",\r\n            \"name\": \"系统二级报警汇总\"\r\n        }\r\n    ]\r\n}', '{\r\n    \"AggType\": 3,\r\n    \"CalcMode\": 1,\r\n    \"Key\": \"\",\r\n    \"SourceCount\": 1,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-1-6\",\r\n            \"name\": \"系统三级报警汇总\"\r\n        }\r\n    ]\r\n}');
INSERT INTO `bind_web_bms` (`id`, `total_voltage`, `total_current`, `online_rack_number`, `fault`, `alarm`, `warning`) VALUES (2, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-2-9\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-2-10\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-2-47\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\r\n    \"AggType\": 3,\r\n    \"CalcMode\": 1,\r\n    \"Key\": \"\",\r\n    \"SourceCount\": 2,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-2-5\",\r\n            \"name\": \"总控报警状态1\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-2-8\",\r\n            \"name\": \"系统一级报警汇总\"\r\n        }\r\n    ]\r\n}', '{\r\n    \"AggType\": 3,\r\n    \"CalcMode\": 1,\r\n    \"Key\": \"\",\r\n    \"SourceCount\": 1,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-2-7\",\r\n            \"name\": \"系统二级报警汇总\"\r\n        }\r\n    ]\r\n}', '{\r\n    \"AggType\": 3,\r\n    \"CalcMode\": 1,\r\n    \"Key\": \"\",\r\n    \"SourceCount\": 1,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-2-6\",\r\n            \"name\": \"系统三级报警汇总\"\r\n        }\r\n    ]\r\n}');
INSERT INTO `bind_web_bms` (`id`, `total_voltage`, `total_current`, `online_rack_number`, `fault`, `alarm`, `warning`) VALUES (3, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-3-9\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-3-10\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-3-47\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\r\n    \"AggType\": 3,\r\n    \"CalcMode\": 1,\r\n    \"Key\": \"\",\r\n    \"SourceCount\": 2,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-3-5\",\r\n            \"name\": \"总控报警状态1\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-3-8\",\r\n            \"name\": \"系统一级报警汇总\"\r\n        }\r\n    ]\r\n}', '{\r\n    \"AggType\": 3,\r\n    \"CalcMode\": 1,\r\n    \"Key\": \"\",\r\n    \"SourceCount\": 1,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-3-7\",\r\n            \"name\": \"系统二级报警汇总\"\r\n        }\r\n    ]\r\n}', '{\r\n    \"AggType\": 3,\r\n    \"CalcMode\": 1,\r\n    \"Key\": \"\",\r\n    \"SourceCount\": 1,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-3-6\",\r\n            \"name\": \"系统三级报警汇总\"\r\n        }\r\n    ]\r\n}');
INSERT INTO `bind_web_bms` (`id`, `total_voltage`, `total_current`, `online_rack_number`, `fault`, `alarm`, `warning`) VALUES (4, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-4-9\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-4-10\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-4-47\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\r\n    \"AggType\": 3,\r\n    \"CalcMode\": 1,\r\n    \"Key\": \"\",\r\n    \"SourceCount\": 2,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-4-5\",\r\n            \"name\": \"总控报警状态1\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-4-8\",\r\n            \"name\": \"系统一级报警汇总\"\r\n        }\r\n    ]\r\n}', '{\r\n    \"AggType\": 3,\r\n    \"CalcMode\": 1,\r\n    \"Key\": \"\",\r\n    \"SourceCount\": 1,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-4-7\",\r\n            \"name\": \"系统二级报警汇总\"\r\n        }\r\n    ]\r\n}', '{\r\n    \"AggType\": 3,\r\n    \"CalcMode\": 1,\r\n    \"Key\": \"\",\r\n    \"SourceCount\": 1,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-4-6\",\r\n            \"name\": \"系统三级报警汇总\"\r\n        }\r\n    ]\r\n}');
INSERT INTO `bind_web_bms` (`id`, `total_voltage`, `total_current`, `online_rack_number`, `fault`, `alarm`, `warning`) VALUES (5, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-5-9\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-5-10\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-5-47\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\r\n    \"AggType\": 3,\r\n    \"CalcMode\": 1,\r\n    \"Key\": \"\",\r\n    \"SourceCount\": 2,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-5-5\",\r\n            \"name\": \"总控报警状态1\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-5-8\",\r\n            \"name\": \"系统一级报警汇总\"\r\n        }\r\n    ]\r\n}', '{\r\n    \"AggType\": 3,\r\n    \"CalcMode\": 1,\r\n    \"Key\": \"\",\r\n    \"SourceCount\": 1,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-5-7\",\r\n            \"name\": \"系统二级报警汇总\"\r\n        }\r\n    ]\r\n}', '{\r\n    \"AggType\": 3,\r\n    \"CalcMode\": 1,\r\n    \"Key\": \"\",\r\n    \"SourceCount\": 1,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-5-6\",\r\n            \"name\": \"系统三级报警汇总\"\r\n        }\r\n    ]\r\n}');
INSERT INTO `bind_web_bms` (`id`, `total_voltage`, `total_current`, `online_rack_number`, `fault`, `alarm`, `warning`) VALUES (6, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-6-9\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-6-10\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-6-47\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\r\n    \"AggType\": 3,\r\n    \"CalcMode\": 1,\r\n    \"Key\": \"\",\r\n    \"SourceCount\": 2,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-6-5\",\r\n            \"name\": \"总控报警状态1\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-6-8\",\r\n            \"name\": \"系统一级报警汇总\"\r\n        }\r\n    ]\r\n}', '{\r\n    \"AggType\": 3,\r\n    \"CalcMode\": 1,\r\n    \"Key\": \"\",\r\n    \"SourceCount\": 1,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-6-7\",\r\n            \"name\": \"系统二级报警汇总\"\r\n        }\r\n    ]\r\n}', '{\r\n    \"AggType\": 3,\r\n    \"CalcMode\": 1,\r\n    \"Key\": \"\",\r\n    \"SourceCount\": 1,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-6-6\",\r\n            \"name\": \"系统三级报警汇总\"\r\n        }\r\n    ]\r\n}');
INSERT INTO `bind_web_bms` (`id`, `total_voltage`, `total_current`, `online_rack_number`, `fault`, `alarm`, `warning`) VALUES (7, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-7-9\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-7-10\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-7-47\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\r\n    \"AggType\": 3,\r\n    \"CalcMode\": 1,\r\n    \"Key\": \"\",\r\n    \"SourceCount\": 2,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-7-5\",\r\n            \"name\": \"总控报警状态1\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-7-8\",\r\n            \"name\": \"系统一级报警汇总\"\r\n        }\r\n    ]\r\n}', '{\r\n    \"AggType\": 3,\r\n    \"CalcMode\": 1,\r\n    \"Key\": \"\",\r\n    \"SourceCount\": 1,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-7-7\",\r\n            \"name\": \"系统二级报警汇总\"\r\n        }\r\n    ]\r\n}', '{\r\n    \"AggType\": 3,\r\n    \"CalcMode\": 1,\r\n    \"Key\": \"\",\r\n    \"SourceCount\": 1,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-7-6\",\r\n            \"name\": \"系统三级报警汇总\"\r\n        }\r\n    ]\r\n}');
INSERT INTO `bind_web_bms` (`id`, `total_voltage`, `total_current`, `online_rack_number`, `fault`, `alarm`, `warning`) VALUES (8, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-8-9\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-8-10\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-8-47\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\r\n    \"AggType\": 3,\r\n    \"CalcMode\": 1,\r\n    \"Key\": \"\",\r\n    \"SourceCount\": 2,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-8-5\",\r\n            \"name\": \"总控报警状态1\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-8-8\",\r\n            \"name\": \"系统一级报警汇总\"\r\n        }\r\n    ]\r\n}', '{\r\n    \"AggType\": 3,\r\n    \"CalcMode\": 1,\r\n    \"Key\": \"\",\r\n    \"SourceCount\": 1,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-8-7\",\r\n            \"name\": \"系统二级报警汇总\"\r\n        }\r\n    ]\r\n}', '{\r\n    \"AggType\": 3,\r\n    \"CalcMode\": 1,\r\n    \"Key\": \"\",\r\n    \"SourceCount\": 1,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-8-6\",\r\n            \"name\": \"系统三级报警汇总\"\r\n        }\r\n    ]\r\n}');
INSERT INTO `bind_web_bms` (`id`, `total_voltage`, `total_current`, `online_rack_number`, `fault`, `alarm`, `warning`) VALUES (9, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-9-9\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-9-10\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-9-47\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\r\n    \"AggType\": 3,\r\n    \"CalcMode\": 1,\r\n    \"Key\": \"\",\r\n    \"SourceCount\": 2,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-9-5\",\r\n            \"name\": \"总控报警状态1\"\r\n        },\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-9-8\",\r\n            \"name\": \"系统一级报警汇总\"\r\n        }\r\n    ]\r\n}', '{\r\n    \"AggType\": 3,\r\n    \"CalcMode\": 1,\r\n    \"Key\": \"\",\r\n    \"SourceCount\": 1,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-9-7\",\r\n            \"name\": \"系统二级报警汇总\"\r\n        }\r\n    ]\r\n}', '{\r\n    \"AggType\": 3,\r\n    \"CalcMode\": 1,\r\n    \"Key\": \"\",\r\n    \"SourceCount\": 1,\r\n    \"Sources\": [\r\n        {\r\n            \"BitIndex\": 0,\r\n            \"Coeff\": 1.0,\r\n            \"Offset\": 0,\r\n            \"Point\": \"yc-9-6\",\r\n            \"name\": \"系统三级报警汇总\"\r\n        }\r\n    ]\r\n}');
COMMIT;

-- ----------------------------
-- Table structure for bind_web_meter
-- ----------------------------
DROP TABLE IF EXISTS `bind_web_meter`;
CREATE TABLE `bind_web_meter` (
  `id` int(11) NOT NULL,
  `a_phase_current` varchar(250) NOT NULL,
  `b_phase_current` varchar(250) NOT NULL,
  `c_phase_current` varchar(250) NOT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of bind_web_meter
-- ----------------------------
BEGIN;
INSERT INTO `bind_web_meter` (`id`, `a_phase_current`, `b_phase_current`, `c_phase_current`) VALUES (15, '{\n    \"CalcMode\": 2,\n    \"Key\": \"\",\n    \"MergeInfo\": {\n        \"Coeff\": 0.001,\n        \"DataType\": 3,\n        \"HighPoint\": \"yc-15-12\",\n        \"LowPoint\": \"yc-15-13\"\n    }\n}', '{\n    \"CalcMode\": 2,\n    \"Key\": \"\",\n    \"MergeInfo\": {\n        \"Coeff\": 0.001,\n        \"DataType\": 3,\n        \"HighPoint\": \"yc-15-14\",\n        \"LowPoint\": \"yc-15-15\"\n    }\n}', '{\n    \"CalcMode\": 2,\n    \"Key\": \"\",\n    \"MergeInfo\": {\n        \"Coeff\": 0.001,\n        \"DataType\": 3,\n        \"HighPoint\": \"yc-15-16\",\n        \"LowPoint\": \"yc-15-17\"\n    }\n}');
COMMIT;

-- ----------------------------
-- Table structure for bind_web_pcs
-- ----------------------------
DROP TABLE IF EXISTS `bind_web_pcs`;
CREATE TABLE `bind_web_pcs` (
  `id` int(11) NOT NULL,
  `hvcb_status` varchar(250) NOT NULL,
  `fault` text NOT NULL,
  `alarm` text NOT NULL,
  `warning` text NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of bind_web_pcs
-- ----------------------------
BEGIN;
INSERT INTO `bind_web_pcs` (`id`, `hvcb_status`, `fault`, `alarm`, `warning`) VALUES (10, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-10-92\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"AggType\": 3,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 10,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-10-10\",\n            \"name\": \"故障位1\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-10-11\",\n            \"name\": \"故障位2\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-10-12\",\n            \"name\": \"故障位3\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-10-13\",\n            \"name\": \"故障位4\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-10-14\",\n            \"name\": \"故障位5\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-10-15\",\n            \"name\": \"故障位6\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-10-16\",\n            \"name\": \"故障位7\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-10-17\",\n            \"name\": \"故障位8\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-10-18\",\n            \"name\": \"故障位9\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-10-19\",\n            \"name\": \"故障位10\"\n        }\n    ]\n}', '', '');
INSERT INTO `bind_web_pcs` (`id`, `hvcb_status`, `fault`, `alarm`, `warning`) VALUES (11, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-11-92\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"AggType\": 3,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 10,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-11-10\",\n            \"name\": \"故障位1\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-11-11\",\n            \"name\": \"故障位2\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-11-12\",\n            \"name\": \"故障位3\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-11-13\",\n            \"name\": \"故障位4\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-11-14\",\n            \"name\": \"故障位5\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-11-15\",\n            \"name\": \"故障位6\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-11-16\",\n            \"name\": \"故障位7\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-11-17\",\n            \"name\": \"故障位8\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-11-18\",\n            \"name\": \"故障位9\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-11-19\",\n            \"name\": \"故障位10\"\n        }\n    ]\n}', '', '');
INSERT INTO `bind_web_pcs` (`id`, `hvcb_status`, `fault`, `alarm`, `warning`) VALUES (12, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-12-92\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"AggType\": 3,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 10,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-10\",\n            \"name\": \"故障位1\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-11\",\n            \"name\": \"故障位2\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-12\",\n            \"name\": \"故障位3\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-13\",\n            \"name\": \"故障位4\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-14\",\n            \"name\": \"故障位5\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-15\",\n            \"name\": \"故障位6\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-16\",\n            \"name\": \"故障位7\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-17\",\n            \"name\": \"故障位8\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-18\",\n            \"name\": \"故障位9\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-19\",\n            \"name\": \"故障位10\"\n        }\n    ]\n}', '', '');
INSERT INTO `bind_web_pcs` (`id`, `hvcb_status`, `fault`, `alarm`, `warning`) VALUES (13, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-13-92\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"AggType\": 3,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 10,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-13-10\",\n            \"name\": \"故障位1\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-13-11\",\n            \"name\": \"故障位2\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-13-12\",\n            \"name\": \"故障位3\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-13-13\",\n            \"name\": \"故障位4\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-13-14\",\n            \"name\": \"故障位5\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-13-15\",\n            \"name\": \"故障位6\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-13-16\",\n            \"name\": \"故障位7\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-13-17\",\n            \"name\": \"故障位8\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-13-18\",\n            \"name\": \"故障位9\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-13-19\",\n            \"name\": \"故障位10\"\n        }\n    ]\n}', '', '');
INSERT INTO `bind_web_pcs` (`id`, `hvcb_status`, `fault`, `alarm`, `warning`) VALUES (14, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-14-92\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 1.0,\n    \"SingleOpType\": 0\n}', '{\n    \"AggType\": 3,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 10,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-10\",\n            \"name\": \"故障位1\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-11\",\n            \"name\": \"故障位2\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-12\",\n            \"name\": \"故障位3\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-13\",\n            \"name\": \"故障位4\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-14\",\n            \"name\": \"故障位5\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-15\",\n            \"name\": \"故障位6\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-16\",\n            \"name\": \"故障位7\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-17\",\n            \"name\": \"故障位8\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-18\",\n            \"name\": \"故障位9\"\n        },\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-19\",\n            \"name\": \"故障位10\"\n        }\n    ]\n}', '', '');
COMMIT;

-- ----------------------------
-- Table structure for bind_web_pcsbranch
-- ----------------------------
DROP TABLE IF EXISTS `bind_web_pcsbranch`;
CREATE TABLE `bind_web_pcsbranch` (
  `id` int(11) NOT NULL,
  `branch_id` int(11) NOT NULL,
  `voltage_ab` varchar(250) NOT NULL,
  `voltage_bc` varchar(250) NOT NULL,
  `voltage_ca` varchar(250) NOT NULL,
  `a_phase_current` varchar(250) NOT NULL,
  `b_phase_current` varchar(250) NOT NULL,
  `c_phase_current` varchar(250) NOT NULL,
  `fault` text NOT NULL,
  `alarm` text NOT NULL,
  `warning` text NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of bind_web_pcsbranch
-- ----------------------------
BEGIN;
INSERT INTO `bind_web_pcsbranch` (`id`, `branch_id`, `voltage_ab`, `voltage_bc`, `voltage_ca`, `a_phase_current`, `b_phase_current`, `c_phase_current`, `fault`, `alarm`, `warning`) VALUES (10, 1, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-10-20\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-10-21\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-10-22\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-10-25\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-10-26\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-10-27\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"AggType\": 3,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 1,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-10-55\",\n            \"name\": \"单元1当前故障\"\n        }\n    ]\n}', '{\n    \"AggType\": 3,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 1,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-10-54\",\n            \"name\": \"单元1当前告警\"\n        }\n    ]\n}', '');
INSERT INTO `bind_web_pcsbranch` (`id`, `branch_id`, `voltage_ab`, `voltage_bc`, `voltage_ca`, `a_phase_current`, `b_phase_current`, `c_phase_current`, `fault`, `alarm`, `warning`) VALUES (10, 2, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-10-56\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-10-57\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-10-58\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-10-61\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-10-62\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-10-63\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"AggType\": 3,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 1,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-10-91\",\n            \"name\": \"单元2当前故障\"\n        }\n    ]\n}', '{\n    \"AggType\": 3,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 1,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-10-90\",\n            \"name\": \"单元2当前告警\"\n        }\n    ]\n}', '');
INSERT INTO `bind_web_pcsbranch` (`id`, `branch_id`, `voltage_ab`, `voltage_bc`, `voltage_ca`, `a_phase_current`, `b_phase_current`, `c_phase_current`, `fault`, `alarm`, `warning`) VALUES (11, 1, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-11-20\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-11-21\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-11-22\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-11-25\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-11-26\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-11-27\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"AggType\": 3,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 1,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-11-55\",\n            \"name\": \"单元1当前故障\"\n        }\n    ]\n}', '{\n    \"AggType\": 3,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 1,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-11-54\",\n            \"name\": \"单元1当前告警\"\n        }\n    ]\n}', '');
INSERT INTO `bind_web_pcsbranch` (`id`, `branch_id`, `voltage_ab`, `voltage_bc`, `voltage_ca`, `a_phase_current`, `b_phase_current`, `c_phase_current`, `fault`, `alarm`, `warning`) VALUES (11, 2, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-11-56\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-11-57\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-11-58\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-11-61\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-11-62\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-11-63\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"AggType\": 3,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 1,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-11-91\",\n            \"name\": \"单元2当前故障\"\n        }\n    ]\n}', '{\n    \"AggType\": 3,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 1,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-11-90\",\n            \"name\": \"单元2当前告警\"\n        }\n    ]\n}', '');
INSERT INTO `bind_web_pcsbranch` (`id`, `branch_id`, `voltage_ab`, `voltage_bc`, `voltage_ca`, `a_phase_current`, `b_phase_current`, `c_phase_current`, `fault`, `alarm`, `warning`) VALUES (12, 1, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-12-20\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-12-21\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-12-22\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-12-25\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-12-26\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-12-27\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"AggType\": 3,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 1,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-55\",\n            \"name\": \"单元1当前故障\"\n        }\n    ]\n}', '{\n    \"AggType\": 3,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 1,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-54\",\n            \"name\": \"单元1当前告警\"\n        }\n    ]\n}', '');
INSERT INTO `bind_web_pcsbranch` (`id`, `branch_id`, `voltage_ab`, `voltage_bc`, `voltage_ca`, `a_phase_current`, `b_phase_current`, `c_phase_current`, `fault`, `alarm`, `warning`) VALUES (12, 2, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-12-56\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-12-57\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-12-58\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-12-61\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-12-62\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-12-63\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"AggType\": 3,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 1,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-91\",\n            \"name\": \"单元2当前故障\"\n        }\n    ]\n}', '{\n    \"AggType\": 3,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 1,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-12-90\",\n            \"name\": \"单元2当前告警\"\n        }\n    ]\n}', '');
INSERT INTO `bind_web_pcsbranch` (`id`, `branch_id`, `voltage_ab`, `voltage_bc`, `voltage_ca`, `a_phase_current`, `b_phase_current`, `c_phase_current`, `fault`, `alarm`, `warning`) VALUES (13, 1, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-13-20\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-13-21\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-13-22\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-13-25\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-13-26\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-13-27\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"AggType\": 3,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 1,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-13-55\",\n            \"name\": \"单元1当前故障\"\n        }\n    ]\n}', '{\n    \"AggType\": 3,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 1,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-13-54\",\n            \"name\": \"单元1当前告警\"\n        }\n    ]\n}', '');
INSERT INTO `bind_web_pcsbranch` (`id`, `branch_id`, `voltage_ab`, `voltage_bc`, `voltage_ca`, `a_phase_current`, `b_phase_current`, `c_phase_current`, `fault`, `alarm`, `warning`) VALUES (13, 2, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-13-56\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-13-57\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-13-58\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-13-61\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-13-62\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-13-63\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"AggType\": 3,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 1,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-13-91\",\n            \"name\": \"单元2当前故障\"\n        }\n    ]\n}', '{\n    \"AggType\": 3,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 1,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-13-90\",\n            \"name\": \"单元2当前告警\"\n        }\n    ]\n}', '');
INSERT INTO `bind_web_pcsbranch` (`id`, `branch_id`, `voltage_ab`, `voltage_bc`, `voltage_ca`, `a_phase_current`, `b_phase_current`, `c_phase_current`, `fault`, `alarm`, `warning`) VALUES (14, 1, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-14-20\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-14-21\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-14-22\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-14-25\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-14-26\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-14-27\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"AggType\": 3,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 1,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-55\",\n            \"name\": \"单元1当前故障\"\n        }\n    ]\n}', '{\n    \"AggType\": 3,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 1,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-54\",\n            \"name\": \"单元1当前告警\"\n        }\n    ]\n}', '');
INSERT INTO `bind_web_pcsbranch` (`id`, `branch_id`, `voltage_ab`, `voltage_bc`, `voltage_ca`, `a_phase_current`, `b_phase_current`, `c_phase_current`, `fault`, `alarm`, `warning`) VALUES (14, 2, '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-14-56\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-14-57\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 0,\n    \"Key\": \"yc-14-58\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-14-61\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-14-62\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"CalcMode\": 0,\n    \"DataType\": 1,\n    \"Key\": \"yc-14-63\",\n    \"Offset\": 0,\n    \"Scale_Factor\": 0.1,\n    \"SingleOpType\": 0\n}', '{\n    \"AggType\": 3,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 1,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-91\",\n            \"name\": \"单元2当前故障\"\n        }\n    ]\n}', '{\n    \"AggType\": 3,\n    \"CalcMode\": 1,\n    \"Key\": \"\",\n    \"SourceCount\": 1,\n    \"Sources\": [\n        {\n            \"BitIndex\": 0,\n            \"Coeff\": 1.0,\n            \"Offset\": 0,\n            \"Point\": \"yc-14-90\",\n            \"name\": \"单元2当前告警\"\n        }\n    ]\n}', '');
COMMIT;

-- ----------------------------
-- Table structure for bms_modbus_bmser_v1_1
-- ----------------------------
DROP TABLE IF EXISTS `bms_modbus_bmser_v1_1`;
CREATE TABLE `bms_modbus_bmser_v1_1` (
  `tag_id` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '点位代号',
  `name` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '点位名称',
  `src_addr` int(11) NOT NULL COMMENT '源地址',
  `func_code` int(11) NOT NULL COMMENT '功能码',
  `slave_addr` int(11) NOT NULL COMMENT '从站地址',
  `quantity` int(11) NOT NULL COMMENT '连续数量',
  `read_write` int(11) NOT NULL COMMENT '读写属性',
  `transmit` int(11) NOT NULL COMMENT '是否变送',
  `forward` int(11) NOT NULL COMMENT '是否转发',
  `remark` text COLLATE utf8mb4_unicode_ci COMMENT '备注'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of bms_modbus_bmser_v1_1
-- ----------------------------
BEGIN;
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '一键并网状态0未开始1进行中2成功3失败', 10000, 4, 1, 2, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yx', 'Rack启用/停用状态', 1001, 5, 1, 1, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yx', 'RACK 在线状态', 1002, 5, 1, 54, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yx', '系统运行状态', 1003, 5, 1, 53, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '系统充放电状态', 10001, 4, 1, 52, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yx', '总控报警状态1', 1004, 5, 1, 51, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '系统三级报警汇总', 10002, 4, 1, 50, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '系统二级报警汇总', 10003, 4, 1, 49, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '系统一级报警汇总', 10004, 4, 1, 48, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '系统总电压', 10005, 4, 1, 47, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '系统总电流', 10006, 4, 1, 46, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '系统SOC', 10007, 4, 1, 45, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '系统SOH', 10008, 4, 1, 44, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '系统绝缘值', 10009, 4, 1, 43, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '系统可充电量', 10010, 4, 1, 42, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '系统可放电量', 10011, 4, 1, 41, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '系统允许最大充电电流', 10012, 4, 1, 40, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '系统允许最大放电电流', 10013, 4, 1, 39, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '簇间电流差异值', 10014, 4, 1, 38, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '簇间总压差异值', 10015, 4, 1, 37, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '系统最高电压电池所在Rack号', 10016, 4, 1, 36, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '系统最高电压电池所在组', 10017, 4, 1, 35, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '系统最高电压电池所在位置', 10018, 4, 1, 34, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '系统最高电池电压', 10019, 4, 1, 33, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '系统最低电压电池所在Rack号', 10020, 4, 1, 32, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '系统最低电压电池所在组', 10021, 4, 1, 31, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '系统最低电压电池所在位置', 10022, 4, 1, 30, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '系统最低电池电压', 10023, 4, 1, 29, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '系统平均电压', 10024, 4, 1, 28, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '系统最高温度电池所在Rack号', 10025, 4, 1, 27, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '系统最高温度电池所在组', 10026, 4, 1, 26, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '系统最高温度电池所在位置', 10027, 4, 1, 25, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '系统最高电池温度', 10028, 4, 1, 24, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '系统最低温度电池所在Rack号', 10029, 4, 1, 23, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '系统最低温度电池所在组', 10030, 4, 1, 22, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '系统最低温度电池所在位置', 10031, 4, 1, 21, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '系统最低电池温度', 10032, 4, 1, 20, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '系统平均温度', 10033, 4, 1, 19, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '主控通信故障报警', 10034, 4, 1, 18, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '主控通信故障报警2', 10035, 4, 1, 17, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'BAU输出干接点状态', 10036, 4, 1, 16, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'Rack启用/停用状态 2', 10037, 4, 1, 15, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 19941, 4, 1, 14, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 19942, 4, 1, 13, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'RACK在线状态2', 10038, 4, 1, 12, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '累计充电电量_H', 10039, 4, 1, 11, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '累计充电电量_L', 10040, 4, 1, 10, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '累计放电电量_H', 10041, 4, 1, 9, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '累计放电电量_L', 10042, 4, 1, 8, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '当前在网簇数', 10043, 4, 1, 7, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '堆内总簇数', 10044, 4, 1, 6, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '最小并机簇数', 10045, 4, 1, 5, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '最大允许放电功率', 10046, 4, 1, 4, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '最大允许充电功率', 10047, 4, 1, 3, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'BMS心跳', 10048, 4, 1, 2, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'Rack 故障汇总', 10049, 4, 1, 1, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '水泵运行状态', 10050, 4, 1, 43, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '1#压缩机运行状态', 10051, 4, 1, 42, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '2#压缩机运行状态', 10052, 4, 1, 41, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '电加热运行状态', 10053, 4, 1, 40, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '出水温度', 10054, 4, 1, 39, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '回水温度', 10055, 4, 1, 38, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '出水压力', 10056, 4, 1, 37, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '回水压力', 10057, 4, 1, 36, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'TMS报警1', 10058, 4, 1, 35, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'TMS报警2', 10059, 4, 1, 34, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '液冷通信状态', 10060, 4, 1, 33, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '冷凝风机运行状态', 10061, 4, 1, 32, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '空调整机状态', 10062, 4, 1, 31, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '空调内风机状态', 10063, 4, 1, 30, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '空调外风机状态', 10064, 4, 1, 29, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '空调压缩机状态', 10065, 4, 1, 28, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '空调电加热状态', 10066, 4, 1, 27, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 10067, 4, 1, 26, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '空调盘管温度', 10067, 4, 1, 25, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 10068, 4, 1, 24, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '空调冷凝温度', 10068, 4, 1, 23, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '空调柜内温度', 10069, 4, 1, 22, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '空调柜内湿度', 10070, 4, 1, 21, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 10071, 4, 1, 20, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 10072, 4, 1, 19, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 10073, 4, 1, 18, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 10074, 4, 1, 17, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '空调告警信息1', 10071, 4, 1, 16, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '空调告警信息2', 10072, 4, 1, 15, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '空调告警信息3', 10073, 4, 1, 14, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '空调制冷点', 10074, 4, 1, 13, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '空调制冷回差', 10075, 4, 1, 12, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '空调加热点', 10076, 4, 1, 11, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '空调加热回差', 10077, 4, 1, 10, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 10078, 4, 1, 9, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 10079, 4, 1, 8, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '空调高温点', 10078, 4, 1, 7, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '空调低温点', 10079, 4, 1, 6, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '空调高湿点', 10080, 4, 1, 5, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '空调内风机停止点', 10081, 4, 1, 4, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '空调监控开关机', 10082, 4, 1, 3, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '空调版本号', 10083, 4, 1, 2, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '空调通信状态', 10084, 4, 1, 1, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '除湿器1控温开启值', 10085, 4, 1, 66, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '除湿器1控温停止值', 10086, 4, 1, 65, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '除湿器1控湿开启值', 10087, 4, 1, 64, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '除湿器1控湿停止值', 10088, 4, 1, 63, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '除湿器1温度报警上限值', 10089, 4, 1, 62, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '除湿器1温度报警下限值', 10090, 4, 1, 61, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 10091, 4, 1, 60, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '除湿器1环境温度值', 10091, 4, 1, 59, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '除湿器1环境湿度值', 10092, 4, 1, 58, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '除湿器1通信故障', 10093, 4, 1, 57, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '除湿器2控温开启值', 10094, 4, 1, 56, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '除湿器2控温停止值', 10095, 4, 1, 55, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '除湿器2控湿开启值', 10096, 4, 1, 54, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '除湿器2控湿停止值', 10097, 4, 1, 53, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '除湿器2温度报警上限值', 10098, 4, 1, 52, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '除湿器2温度报警下限值', 10099, 4, 1, 51, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 10100, 4, 1, 50, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '除湿器2环境温度值', 10100, 4, 1, 49, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '除湿器2环境湿度值', 10101, 4, 1, 48, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '除湿器2通信故障', 10102, 4, 1, 47, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '除湿器3控温开启值', 10103, 4, 1, 46, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '除湿器3控温停止值', 10104, 4, 1, 45, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '除湿器3控湿开启值', 10105, 4, 1, 44, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '除湿器3控湿停止值', 10106, 4, 1, 43, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '除湿器3温度报警上限值', 10107, 4, 1, 42, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '除湿器3温度报警下限值', 10108, 4, 1, 41, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 10109, 4, 1, 40, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '除湿器3环境温度值', 10109, 4, 1, 39, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '除湿器3环境湿度值', 10110, 4, 1, 38, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '除湿器3通信故障', 10111, 4, 1, 37, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'CO浓度', 10112, 4, 1, 36, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 10113, 4, 1, 35, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '故障和告警', 10114, 4, 1, 34, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '监控器状态监控中(40107)', 10125, 4, 1, 33, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '监控器状态监控中', 10116, 4, 1, 32, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '心跳(40108)', 10126, 4, 1, 31, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '校准到期(40109 to 40110)_H', 10127, 4, 1, 30, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '校准到期(40109 to 40110)_L', 10128, 4, 1, 29, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '可燃气体探测通讯故障', 10128, 4, 1, 28, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '气表读数(40103 to 40104)_H', 10121, 4, 1, 27, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '气表读数(40103 to 40104)_L', 10122, 4, 1, 26, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '故障和告警(40105)', 10123, 4, 1, 25, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '报警，故障和告警状态(40106)', 10124, 4, 1, 24, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '监控器状态监控中(40107)', 10125, 4, 1, 23, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '心跳(40108)', 10126, 4, 1, 22, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '校准到期(40109 to 40110)', 10127, 4, 1, 21, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 10128, 4, 1, 20, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '可燃气体探测通讯故障', 10128, 4, 1, 19, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '气体浓度告警', 10129, 4, 1, 18, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 10130, 4, 1, 17, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 10131, 4, 1, 16, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 10132, 4, 1, 15, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 10133, 4, 1, 14, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 10134, 4, 1, 13, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 10135, 4, 1, 12, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 10136, 4, 1, 11, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 10137, 4, 1, 10, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 10138, 4, 1, 9, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 10139, 4, 1, 8, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 10140, 4, 1, 7, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 10141, 4, 1, 6, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 10142, 4, 1, 5, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 10143, 4, 1, 4, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 10144, 4, 1, 3, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'TCP扩展IO状态', 10130, 4, 1, 2, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'TCP扩展IO通讯故障', 10131, 4, 1, 1, 0, 0, 0, NULL);
INSERT INTO `bms_modbus_bmser_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yk', '一键并网开关(写1触发)', 1000, 5, 1, 1, 0, 0, 0, NULL);
COMMIT;

-- ----------------------------
-- Table structure for channel_GOOSE
-- ----------------------------
DROP TABLE IF EXISTS `channel_GOOSE`;
CREATE TABLE `channel_GOOSE` (
  `id` int(11) NOT NULL,
  `name` varchar(255) NOT NULL,
  `protocol` int(11) NOT NULL,
  `mount_table` varchar(255) DEFAULT NULL,
  `enable` int(11) NOT NULL,
  `port_number` int(11) NOT NULL,
  `mac` varchar(255) DEFAULT NULL,
  `goid` varchar(255) DEFAULT NULL,
  `gocbref` varchar(255) DEFAULT NULL,
  `dataset` varchar(255) DEFAULT NULL,
  `appid` int(11) NOT NULL,
  `data_number` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of channel_GOOSE
-- ----------------------------
BEGIN;
INSERT INTO `channel_GOOSE` (`id`, `name`, `protocol`, `mount_table`, `enable`, `port_number`, `mac`, `goid`, `gocbref`, `dataset`, `appid`, `data_number`) VALUES (0, '有功', 3, 'aaa', 1, 4, '01:0c:cd:01:00:00', '1000', 'China_PCSPIGO/LLN0$GO$gocb1', 'China_PCSPIGO/LLN0$GO$gocb1', 10, 10);
INSERT INTO `channel_GOOSE` (`id`, `name`, `protocol`, `mount_table`, `enable`, `port_number`, `mac`, `goid`, `gocbref`, `dataset`, `appid`, `data_number`) VALUES (2, '无功', 3, 'aaa', 1, 4, '01:0c:cd:01:00:00', '1000', 'simpleIOGenericIO/LLN0$AnalogValues', 'simpleIOGenericIO/LLN0$AnalogValues', 80, 10);
COMMIT;

-- ----------------------------
-- Table structure for channel_TCP
-- ----------------------------
DROP TABLE IF EXISTS `channel_TCP`;
CREATE TABLE `channel_TCP` (
  `id` varchar(255) DEFAULT NULL,
  `name` varchar(255) DEFAULT NULL,
  `protocol` int(11) DEFAULT NULL,
  `mount_table` varchar(255) DEFAULT NULL,
  `enable` int(11) DEFAULT NULL,
  `ip_a` varchar(255) DEFAULT NULL,
  `ip_b` varchar(255) DEFAULT NULL,
  `port` int(11) DEFAULT NULL,
  `extra_config` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of channel_TCP
-- ----------------------------
BEGIN;
INSERT INTO `channel_TCP` (`id`, `name`, `protocol`, `mount_table`, `enable`, `ip_a`, `ip_b`, `port`, `extra_config`) VALUES ('0', 'simBms1', 1, 'bms_modbus_bmser_v1_1', 1, '10.37.58.26', '192.168.1.137', 1501, '{500,1000}');
INSERT INTO `channel_TCP` (`id`, `name`, `protocol`, `mount_table`, `enable`, `ip_a`, `ip_b`, `port`, `extra_config`) VALUES ('1', 'simBms2', 1, 'bms_modbus_bmser_v1_1', 1, '10.37.58.26', '192.168.1.137', 1502, '{500,1000}');
INSERT INTO `channel_TCP` (`id`, `name`, `protocol`, `mount_table`, `enable`, `ip_a`, `ip_b`, `port`, `extra_config`) VALUES ('2', 'simBms3', 1, 'bms_modbus_bmser_v1_1', 1, '10.37.58.26', '192.168.1.137', 1503, '{500,1000}');
INSERT INTO `channel_TCP` (`id`, `name`, `protocol`, `mount_table`, `enable`, `ip_a`, `ip_b`, `port`, `extra_config`) VALUES ('3', 'simBms4', 1, 'bms_modbus_bmser_v1_1', 1, '10.37.58.26', '192.168.1.137', 1504, '{500,1000}');
INSERT INTO `channel_TCP` (`id`, `name`, `protocol`, `mount_table`, `enable`, `ip_a`, `ip_b`, `port`, `extra_config`) VALUES ('4', 'simBms5', 1, 'bms_modbus_bmser_v1_1', 1, '10.37.58.26', '192.168.1.137', 1505, '{500,1000}');
INSERT INTO `channel_TCP` (`id`, `name`, `protocol`, `mount_table`, `enable`, `ip_a`, `ip_b`, `port`, `extra_config`) VALUES ('5', 'simBms6', 1, 'bms_modbus_bmser_v1_1', 1, '10.37.58.26', '192.168.1.137', 1506, '{500,1000}');
INSERT INTO `channel_TCP` (`id`, `name`, `protocol`, `mount_table`, `enable`, `ip_a`, `ip_b`, `port`, `extra_config`) VALUES ('6', 'simBms7', 1, 'bms_modbus_bmser_v1_1', 1, '10.37.58.26', '192.168.1.137', 1507, '{500,1000}');
INSERT INTO `channel_TCP` (`id`, `name`, `protocol`, `mount_table`, `enable`, `ip_a`, `ip_b`, `port`, `extra_config`) VALUES ('7', 'simBms8', 1, 'bms_modbus_bmser_v1_1', 1, '10.37.58.26', '192.168.1.137', 1508, '{500,1000}');
INSERT INTO `channel_TCP` (`id`, `name`, `protocol`, `mount_table`, `enable`, `ip_a`, `ip_b`, `port`, `extra_config`) VALUES ('8', 'simBms9', 1, 'bms_modbus_bmser_v1_1', 1, '10.37.58.26', '192.168.1.137', 1509, '{500,1000}');
INSERT INTO `channel_TCP` (`id`, `name`, `protocol`, `mount_table`, `enable`, `ip_a`, `ip_b`, `port`, `extra_config`) VALUES ('9', 'simBms10', 1, 'bms_modbus_bmser_v1_1', 1, '10.37.58.26', '192.168.1.137', 1510, '{500,1000}');
INSERT INTO `channel_TCP` (`id`, `name`, `protocol`, `mount_table`, `enable`, `ip_a`, `ip_b`, `port`, `extra_config`) VALUES ('10', 'simEmu1', 1, 'pcs_modbus_trina_v1_1', 1, '10.37.58.26', '192.168.1.137', 1601, '{500,1000}');
INSERT INTO `channel_TCP` (`id`, `name`, `protocol`, `mount_table`, `enable`, `ip_a`, `ip_b`, `port`, `extra_config`) VALUES ('11', 'simEmu2', 1, 'pcs_modbus_trina_v1_1', 1, '10.37.58.26', '192.168.1.137', 1602, '{500,1000}');
INSERT INTO `channel_TCP` (`id`, `name`, `protocol`, `mount_table`, `enable`, `ip_a`, `ip_b`, `port`, `extra_config`) VALUES ('12', 'simEmu3', 1, 'pcs_modbus_trina_v1_1', 1, '10.37.58.26', '192.168.1.137', 1603, '{500,1000}');
INSERT INTO `channel_TCP` (`id`, `name`, `protocol`, `mount_table`, `enable`, `ip_a`, `ip_b`, `port`, `extra_config`) VALUES ('13', 'simEmu4', 1, 'pcs_modbus_trina_v1_1', 1, '10.37.58.26', '192.168.1.137', 1604, '{500,1000}');
INSERT INTO `channel_TCP` (`id`, `name`, `protocol`, `mount_table`, `enable`, `ip_a`, `ip_b`, `port`, `extra_config`) VALUES ('14', 'simEmu5', 1, 'pcs_modbus_trina_v1_1', 1, '10.37.58.26', '192.168.1.137', 1605, '{500,1000}');
INSERT INTO `channel_TCP` (`id`, `name`, `protocol`, `mount_table`, `enable`, `ip_a`, `ip_b`, `port`, `extra_config`) VALUES ('15', 'simEm', 1, 'meter_modbus_ws_v1_1', 1, '10.37.58.26', '192.168.1.137', 1500, '{500,1000}');
COMMIT;

-- ----------------------------
-- Table structure for curve_local_active
-- ----------------------------
DROP TABLE IF EXISTS `curve_local_active`;
CREATE TABLE `curve_local_active` (
  `id` int(11) NOT NULL,
  `enable` int(255) NOT NULL,
  `time_type` int(11) NOT NULL,
  `week` int(255) DEFAULT NULL,
  `date_start` varchar(255) DEFAULT NULL,
  `date_stop` varchar(255) DEFAULT NULL,
  `power_curve` varchar(255) NOT NULL,
  `name` varchar(255) DEFAULT NULL,
  `is_deletion` tinyint(1) DEFAULT NULL,
  `updated_at` int(11) NOT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of curve_local_active
-- ----------------------------
BEGIN;
INSERT INTO `curve_local_active` (`id`, `enable`, `time_type`, `week`, `date_start`, `date_stop`, `power_curve`, `name`, `is_deletion`, `updated_at`) VALUES (1, 1, 0, 31, '', '', '00:00,0;08:00,800;12:00,-8888;14:00,-200;23:59,0', '工作日曲线', 0, 1780026492);
INSERT INTO `curve_local_active` (`id`, `enable`, `time_type`, `week`, `date_start`, `date_stop`, `power_curve`, `name`, `is_deletion`, `updated_at`) VALUES (2, 1, 0, 32, '', '', '00:00,30;10:00,800;18:00,-200;22:00,0', '休息日曲线', 0, 1780034035);
INSERT INTO `curve_local_active` (`id`, `enable`, `time_type`, `week`, `date_start`, `date_stop`, `power_curve`, `name`, `is_deletion`, `updated_at`) VALUES (3, 0, 0, 127, '', '', '00:00,0;08:00,500;12:00,300;14:00,-2500;23:59,0', '工作日曲线', 0, 1780036195);
INSERT INTO `curve_local_active` (`id`, `enable`, `time_type`, `week`, `date_start`, `date_stop`, `power_curve`, `name`, `is_deletion`, `updated_at`) VALUES (4, 0, 1, 0, '2026-05-28', '2026-05-28', '00:00,234', 'test', 0, 1780036209);
INSERT INTO `curve_local_active` (`id`, `enable`, `time_type`, `week`, `date_start`, `date_stop`, `power_curve`, `name`, `is_deletion`, `updated_at`) VALUES (5, 0, 0, 1, '', '', '00:00,1000', '454', 0, 1780039118);
COMMIT;

-- ----------------------------
-- Table structure for curve_local_reactive
-- ----------------------------
DROP TABLE IF EXISTS `curve_local_reactive`;
CREATE TABLE `curve_local_reactive` (
  `id` int(11) NOT NULL,
  `enable` int(255) NOT NULL,
  `time_type` int(11) NOT NULL,
  `week` int(255) DEFAULT NULL,
  `date_start` varchar(255) DEFAULT NULL,
  `date_stop` varchar(255) DEFAULT NULL,
  `power_curve` varchar(255) NOT NULL,
  `name` varchar(255) DEFAULT NULL,
  `is_deletion` tinyint(1) DEFAULT NULL,
  `updated_at` int(11) NOT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of curve_local_reactive
-- ----------------------------
BEGIN;
INSERT INTO `curve_local_reactive` (`id`, `enable`, `time_type`, `week`, `date_start`, `date_stop`, `power_curve`, `name`, `is_deletion`, `updated_at`) VALUES (1, 1, 0, 127, '', '', '00:00,1000;14:44,-3321', '全部', 0, 1780469664);
INSERT INTO `curve_local_reactive` (`id`, `enable`, `time_type`, `week`, `date_start`, `date_stop`, `power_curve`, `name`, `is_deletion`, `updated_at`) VALUES (2, 0, 0, 1, '', '', '00:00,100', '4545', 0, 1780039163);
COMMIT;

-- ----------------------------
-- Table structure for curve_remote_active
-- ----------------------------
DROP TABLE IF EXISTS `curve_remote_active`;
CREATE TABLE `curve_remote_active` (
  `id` int(11) NOT NULL,
  `enable` int(255) NOT NULL,
  `time_type` int(11) NOT NULL,
  `week` int(255) DEFAULT NULL,
  `date_start` varchar(255) DEFAULT NULL,
  `date_stop` varchar(255) DEFAULT NULL,
  `power_curve` varchar(255) NOT NULL,
  `name` varchar(255) DEFAULT NULL,
  `is_deletion` tinyint(1) DEFAULT NULL,
  `updated_at` int(11) NOT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of curve_remote_active
-- ----------------------------
BEGIN;
INSERT INTO `curve_remote_active` (`id`, `enable`, `time_type`, `week`, `date_start`, `date_stop`, `power_curve`, `name`, `is_deletion`, `updated_at`) VALUES (1, 1, 0, 127, '', NULL, '00:00,0;08:00,-4000;12:00,3000;18:00,-200;23:59,0', NULL, 0, 0);
COMMIT;

-- ----------------------------
-- Table structure for curve_remote_reactive
-- ----------------------------
DROP TABLE IF EXISTS `curve_remote_reactive`;
CREATE TABLE `curve_remote_reactive` (
  `id` int(11) NOT NULL,
  `enable` int(255) NOT NULL,
  `time_type` int(11) NOT NULL,
  `week` int(255) DEFAULT NULL,
  `date_start` varchar(255) DEFAULT NULL,
  `date_stop` varchar(255) DEFAULT NULL,
  `power_curve` varchar(255) NOT NULL,
  `name` varchar(255) DEFAULT NULL,
  `is_deletion` tinyint(1) DEFAULT NULL,
  `updated_at` int(11) NOT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of curve_remote_reactive
-- ----------------------------
BEGIN;
INSERT INTO `curve_remote_reactive` (`id`, `enable`, `time_type`, `week`, `date_start`, `date_stop`, `power_curve`, `name`, `is_deletion`, `updated_at`) VALUES (1, 1, 0, 127, NULL, NULL, '00:00,0;08:00,-4000;12:00,3000;18:00,-200;23:59,0', 'test', 0, 0);
COMMIT;

-- ----------------------------
-- Table structure for ems_wave_config
-- ----------------------------
DROP TABLE IF EXISTS `ems_wave_config`;
CREATE TABLE `ems_wave_config` (
  `config_key` varchar(64) NOT NULL COMMENT '配置项键名',
  `config_value` varchar(256) NOT NULL COMMENT '配置项值',
  `config_desc` varchar(256) DEFAULT NULL COMMENT '配置项说明',
  `update_time` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '最后更新时间',
  PRIMARY KEY (`config_key`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of ems_wave_config
-- ----------------------------
BEGIN;
INSERT INTO `ems_wave_config` (`config_key`, `config_value`, `config_desc`, `update_time`) VALUES ('wave_record_enable', '1', '录波功能使能位：0-关闭，1-开启', '2026-04-10 08:41:27');
COMMIT;

-- ----------------------------
-- Table structure for filter
-- ----------------------------
DROP TABLE IF EXISTS `filter`;
CREATE TABLE `filter` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `measure_type` int(11) DEFAULT '0',
  `filter_switch` tinyint(1) DEFAULT '0',
  `filter_coefficient` double DEFAULT '0',
  `updated_at` int(11) DEFAULT '0',
  PRIMARY KEY (`id`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of filter
-- ----------------------------
BEGIN;
INSERT INTO `filter` (`id`, `measure_type`, `filter_switch`, `filter_coefficient`, `updated_at`) VALUES (1, 0, 0, 1, 0);
INSERT INTO `filter` (`id`, `measure_type`, `filter_switch`, `filter_coefficient`, `updated_at`) VALUES (2, 1, 0, 1, 0);
INSERT INTO `filter` (`id`, `measure_type`, `filter_switch`, `filter_coefficient`, `updated_at`) VALUES (3, 2, 0, 1, 0);
INSERT INTO `filter` (`id`, `measure_type`, `filter_switch`, `filter_coefficient`, `updated_at`) VALUES (4, 3, 0, 1, 0);
INSERT INTO `filter` (`id`, `measure_type`, `filter_switch`, `filter_coefficient`, `updated_at`) VALUES (5, 4, 0, 1, 0);
COMMIT;

-- ----------------------------
-- Table structure for home_logo
-- ----------------------------
DROP TABLE IF EXISTS `home_logo`;
CREATE TABLE `home_logo` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `logo` varchar(255) NOT NULL DEFAULT '',
  `emsmodel` varchar(255) NOT NULL DEFAULT '',
  PRIMARY KEY (`id`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of home_logo
-- ----------------------------
BEGIN;
INSERT INTO `home_logo` (`id`, `logo`, `emsmodel`) VALUES (1, 'logo.svg', '4000 - EMS');
COMMIT;

-- ----------------------------
-- Table structure for inertia
-- ----------------------------
DROP TABLE IF EXISTS `inertia`;
CREATE TABLE `inertia` (
  `id` int(11) NOT NULL,
  `enable` tinyint(1) DEFAULT NULL,
  `rated_frequency` double DEFAULT NULL,
  `deadband_frequency_amplitude` double DEFAULT NULL,
  `deadband_frequency_rate` double DEFAULT NULL,
  `constant_t_j` double DEFAULT NULL,
  `max_active_power_export_at_poi` double DEFAULT NULL,
  `max_active_power_import_at_poi` double DEFAULT NULL,
  `export_limit` double DEFAULT NULL,
  `import_limit` double DEFAULT NULL,
  `enable_lock_primary_frequency` tinyint(1) DEFAULT NULL,
  `control_cycle` int(11) DEFAULT NULL,
  `reset_time` int(11) DEFAULT NULL,
  `updated_at` int(11) DEFAULT NULL,
  `max_vaild_frequency_value` double DEFAULT NULL,
  `min_vaild_frequency_value` double DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------
-- Records of inertia
-- ----------------------------
BEGIN;
INSERT INTO `inertia` (`id`, `enable`, `rated_frequency`, `deadband_frequency_amplitude`, `deadband_frequency_rate`, `constant_t_j`, `max_active_power_export_at_poi`, `max_active_power_import_at_poi`, `export_limit`, `import_limit`, `enable_lock_primary_frequency`, `control_cycle`, `reset_time`, `updated_at`, `max_vaild_frequency_value`, `min_vaild_frequency_value`) VALUES (1, 0, 50, 0.04, 0.5, 10, 12500, 12500, 0.3, 0.3, 1, 100, 1000, 1779848287, 51, 49);
COMMIT;

-- ----------------------------
-- Table structure for meter_modbus_ws_v1_1
-- ----------------------------
DROP TABLE IF EXISTS `meter_modbus_ws_v1_1`;
CREATE TABLE `meter_modbus_ws_v1_1` (
  `tag_id` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '点位代号',
  `name` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '点位名称',
  `src_addr` int(11) NOT NULL COMMENT '源地址',
  `func_code` int(11) NOT NULL COMMENT '功能码',
  `slave_addr` int(11) NOT NULL COMMENT '从站地址',
  `quantity` int(11) NOT NULL COMMENT '连续数量',
  `read_write` int(11) NOT NULL COMMENT '读写属性',
  `transmit` int(11) NOT NULL COMMENT '是否变送',
  `forward` int(11) NOT NULL COMMENT '是否转发',
  `remark` text COLLATE utf8mb4_unicode_ci COMMENT '备注'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of meter_modbus_ws_v1_1
-- ----------------------------
BEGIN;
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'A相电压_H', 0, 4, 1, 44, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'A相电压_L', 1, 4, 1, 43, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'B相电压_H', 2, 4, 1, 42, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'B相电压_L', 3, 4, 1, 41, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'C相电压_H', 4, 4, 1, 40, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'C相电压_L', 5, 4, 1, 39, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'AB线电压_H', 6, 4, 1, 38, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'AB线电压_L', 7, 4, 1, 37, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'BC线电压_H', 8, 4, 1, 36, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'BC线电压_L', 9, 4, 1, 35, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'CA线电压_H', 10, 4, 1, 34, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'CA线电压_L', 11, 4, 1, 33, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'A相电流_H', 12, 4, 1, 32, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'A相电流_L', 13, 4, 1, 31, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'B相电流_H', 14, 4, 1, 30, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'B相电流_L', 15, 4, 1, 29, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'C相电流_H', 16, 4, 1, 28, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'C相电流_L', 17, 4, 1, 27, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'A相有功功率_H', 18, 4, 1, 26, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'A相有功功率_L', 19, 4, 1, 25, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'B相有功功率_H', 20, 4, 1, 24, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'B相有功功率_L', 21, 4, 1, 23, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'C相有功功率_H', 22, 4, 1, 22, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'C相有功功率_L', 23, 4, 1, 21, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '总有功功率_H', 24, 4, 1, 20, 0, 1, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '总有功功率_L', 25, 4, 1, 19, 0, 1, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'A相无功功率_H', 26, 4, 1, 18, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'A相无功功率_L', 27, 4, 1, 17, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'B相无功功率_H', 28, 4, 1, 16, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'B相无功功率_L', 29, 4, 1, 15, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'C相无功功率_H', 30, 4, 1, 14, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', 'C相无功功率_L', 31, 4, 1, 13, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '总无功功率_H', 32, 4, 1, 12, 0, 1, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '总无功功率_L', 33, 4, 1, 11, 0, 1, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '总视在功率_H', 34, 4, 1, 10, 0, 1, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '总视在功率_L', 35, 4, 1, 9, 0, 1, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '功率因数_H', 36, 4, 1, 8, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '功率因数_L', 37, 4, 1, 7, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '频率_H', 38, 4, 1, 6, 0, 1, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '频率_L', 39, 4, 1, 5, 0, 1, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '正向总有功电能_H', 40, 4, 1, 4, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '正向总有功电能_L', 41, 4, 1, 3, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '反向总有功电能_H', 42, 4, 1, 2, 0, 0, 0, NULL);
INSERT INTO `meter_modbus_ws_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '反向总有功电能_L', 43, 4, 1, 1, 0, 0, 0, NULL);
COMMIT;

-- ----------------------------
-- Table structure for pcs_modbus_trina_v1_1
-- ----------------------------
DROP TABLE IF EXISTS `pcs_modbus_trina_v1_1`;
CREATE TABLE `pcs_modbus_trina_v1_1` (
  `tag_id` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '点位代号',
  `name` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '点位名称',
  `src_addr` int(11) NOT NULL COMMENT '源地址',
  `func_code` int(11) NOT NULL COMMENT '功能码',
  `slave_addr` int(11) NOT NULL COMMENT '从站地址',
  `quantity` int(11) NOT NULL COMMENT '连续数量',
  `read_write` int(11) NOT NULL COMMENT '读写属性',
  `transmit` int(11) NOT NULL COMMENT '是否变送',
  `forward` int(11) NOT NULL COMMENT '是否转发',
  `remark` text COLLATE utf8mb4_unicode_ci COMMENT '备注'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of pcs_modbus_trina_v1_1
-- ----------------------------
BEGIN;
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '状态位1', 10000, 4, 1, 20, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '状态位2', 10001, 4, 1, 19, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '状态位3', 10002, 4, 1, 18, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '状态位4', 10003, 4, 1, 17, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '状态位5', 10004, 4, 1, 16, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '状态位6', 10005, 4, 1, 15, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '状态位7', 10006, 4, 1, 14, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '状态位8', 10007, 4, 1, 13, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '状态位9', 10008, 4, 1, 12, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '状态位10', 10009, 4, 1, 11, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '故障位1', 10010, 4, 1, 10, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '故障位2', 10011, 4, 1, 9, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '故障位3', 10012, 4, 1, 8, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '故障位4', 10013, 4, 1, 7, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '故障位5', 10014, 4, 1, 6, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '故障位6', 10015, 4, 1, 5, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '故障位7', 10016, 4, 1, 4, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '故障位8', 10017, 4, 1, 3, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '故障位9', 10018, 4, 1, 2, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '故障位10', 10019, 4, 1, 1, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1交流侧AB电压', 10020, 4, 1, 36, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1交流侧BC电压', 10021, 4, 1, 35, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1交流侧CA电压', 10022, 4, 1, 34, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '交流侧频率_H', 10059, 4, 1, 33, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '交流侧频率_L', 10060, 4, 1, 32, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1交流侧A相电流', 10025, 4, 1, 31, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1交流侧B相电流', 10026, 4, 1, 30, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1交流侧C相电流', 10027, 4, 1, 29, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1交流侧有功功率_H', 10028, 4, 1, 28, 0, 1, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1交流侧有功功率_L', 10029, 4, 1, 27, 0, 1, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1交流侧无功功率_H', 10030, 4, 1, 26, 0, 1, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1交流侧无功功率_L', 10031, 4, 1, 25, 0, 1, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1交流侧视在功率_H', 10032, 4, 1, 24, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1交流侧视在功率_L', 10033, 4, 1, 23, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1功率因数', 10034, 4, 1, 22, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1直流侧电压', 10035, 4, 1, 21, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1直流侧电流', 10036, 4, 1, 20, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1直流侧功率', 10037, 4, 1, 19, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 10038, 4, 1, 18, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1 IGBT温度', 10039, 4, 1, 17, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1 舱内温度', 10040, 4, 1, 16, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1最大允许充电功率', 10041, 4, 1, 15, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1最大允许放电功率', 10042, 4, 1, 14, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1最大感性无功', 10043, 4, 1, 13, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1最大容性无功', 10044, 4, 1, 12, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1总充电量_H', 10045, 4, 1, 11, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1总充电量_L', 10046, 4, 1, 10, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1总放电量_H', 10047, 4, 1, 9, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1总放电量_L', 10048, 4, 1, 8, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1当日充电量_H', 10049, 4, 1, 7, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1当日充电量_L', 10050, 4, 1, 6, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1当日放电量_H', 10051, 4, 1, 5, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1当日放电量_L', 10052, 4, 1, 4, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1运行状态', 10053, 4, 1, 3, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1当前告警', 10054, 4, 1, 2, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元1当前故障', 10055, 4, 1, 1, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2交流侧AB电压', 10056, 4, 1, 36, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2交流侧BC电压', 10057, 4, 1, 35, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2交流侧CA电压', 10058, 4, 1, 34, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '交流侧频率_H', 10059, 4, 1, 33, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '交流侧频率_L', 10060, 4, 1, 32, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2交流侧A相电流', 10061, 4, 1, 31, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2交流侧B相电流', 10062, 4, 1, 30, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2交流侧C相电流', 10063, 4, 1, 29, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2交流侧有功功率_H', 10064, 4, 1, 28, 0, 1, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2交流侧有功功率_L', 10065, 4, 1, 27, 0, 1, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2交流侧无功功率_H', 10066, 4, 1, 26, 0, 1, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2交流侧无功功率_L', 10067, 4, 1, 25, 0, 1, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2交流侧视在功率_H', 10068, 4, 1, 24, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2交流侧视在功率_L', 10069, 4, 1, 23, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2功率因数', 10070, 4, 1, 22, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2直流侧电压', 10071, 4, 1, 21, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2直流侧电流', 10072, 4, 1, 20, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2直流侧功率', 10073, 4, 1, 19, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '备用', 10074, 4, 1, 18, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2 IGBT温度', 10075, 4, 1, 17, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2 舱内温度', 10076, 4, 1, 16, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2最大允许充电功率', 10077, 4, 1, 15, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2最大允许放电功率', 10078, 4, 1, 14, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2最大感性无功', 10079, 4, 1, 13, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2最大容性无功', 10080, 4, 1, 12, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2总充电量_H', 10081, 4, 1, 11, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2总充电量_L', 10082, 4, 1, 10, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2总放电量_H', 10083, 4, 1, 9, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2总放电量_L', 10084, 4, 1, 8, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2当日充电量_H', 10085, 4, 1, 7, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2当日充电量_L', 10086, 4, 1, 6, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2当日放电量_H', 10087, 4, 1, 5, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2当日放电量_L', 10088, 4, 1, 4, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2运行状态', 10089, 4, 1, 3, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2当前告警', 10090, 4, 1, 2, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yc', '单元2当前故障', 10091, 4, 1, 1, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yx', '高压断路器开合', 1000, 5, 1, 1, 0, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yk', '单元1有功功率设置值', 40000, 6, 1, 4, 1, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yk', '单元1无功设置值', 40001, 6, 1, 3, 1, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yk', '单元1功率因数设置值', 40002, 6, 1, 2, 1, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yk', '单元1启停', 1003, 5, 1, 1, 1, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yk', '单元2有功功率设置值', 40004, 6, 1, 4, 1, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yk', '单元2无功设置值', 40005, 6, 1, 3, 1, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yk', '单元2功率因数设置值', 40006, 6, 1, 2, 1, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yk', '单元2启停', 1005, 5, 1, 1, 1, 0, 0, NULL);
INSERT INTO `pcs_modbus_trina_v1_1` (`tag_id`, `name`, `src_addr`, `func_code`, `slave_addr`, `quantity`, `read_write`, `transmit`, `forward`, `remark`) VALUES ('yk', '高压断路器', 1000, 5, 1, 1, 1, 0, 0, NULL);
COMMIT;

-- ----------------------------
-- Table structure for pi_control
-- ----------------------------
DROP TABLE IF EXISTS `pi_control`;
CREATE TABLE `pi_control` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `object` int(11) DEFAULT '0',
  `control_cycle` int(11) DEFAULT '0',
  `ratio_factor` double DEFAULT '0',
  `integral_factor` double DEFAULT '0',
  `deadband` double DEFAULT '0',
  `output_up_limit` double DEFAULT '0',
  `output_down_limit` double DEFAULT '0',
  `anti_windup_gain` double DEFAULT '0',
  `updated_at` int(11) DEFAULT '0',
  PRIMARY KEY (`id`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of pi_control
-- ----------------------------
BEGIN;
INSERT INTO `pi_control` (`id`, `object`, `control_cycle`, `ratio_factor`, `integral_factor`, `deadband`, `output_up_limit`, `output_down_limit`, `anti_windup_gain`, `updated_at`) VALUES (1, 0, 6000, 0, 0.15, 0.05, 100, -100, 1, 0);
INSERT INTO `pi_control` (`id`, `object`, `control_cycle`, `ratio_factor`, `integral_factor`, `deadband`, `output_up_limit`, `output_down_limit`, `anti_windup_gain`, `updated_at`) VALUES (2, 1, 6000, 0, 0.15, 0.05, 100, -100, 1, 0);
INSERT INTO `pi_control` (`id`, `object`, `control_cycle`, `ratio_factor`, `integral_factor`, `deadband`, `output_up_limit`, `output_down_limit`, `anti_windup_gain`, `updated_at`) VALUES (3, 2, 6000, 0.9, 0, 0.1, 102.6, 97.4, 0, 0);
COMMIT;

-- ----------------------------
-- Table structure for power_factor
-- ----------------------------
DROP TABLE IF EXISTS `power_factor`;
CREATE TABLE `power_factor` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `value` double DEFAULT '0',
  `updated_at` int(11) DEFAULT '0',
  PRIMARY KEY (`id`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of power_factor
-- ----------------------------
BEGIN;
INSERT INTO `power_factor` (`id`, `value`, `updated_at`) VALUES (1, 90, 123123123);
COMMIT;

-- ----------------------------
-- Table structure for primary_frequency
-- ----------------------------
DROP TABLE IF EXISTS `primary_frequency`;
CREATE TABLE `primary_frequency` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `over_enable` tinyint(1) DEFAULT '0',
  `under_enable` tinyint(1) DEFAULT '0',
  `rated_frequency` double DEFAULT '0',
  `regulation_type` tinyint(1) DEFAULT '0',
  `over_droop_rate_1` double DEFAULT '0',
  `under_droop_rate_1` double DEFAULT '0',
  `deadband_1` double DEFAULT '0',
  `over_droop_rate_2` double DEFAULT '0',
  `under_droop_rate_2` double DEFAULT '0',
  `deadband_2` double DEFAULT '0',
  `max_active_power_export_at_poi` double DEFAULT '0',
  `max_active_power_import_at_poi` double DEFAULT '0',
  `export_limit` double DEFAULT '0',
  `import_limit` double DEFAULT '0',
  `control_cycle` int(11) DEFAULT '0',
  `reset_time` int(11) DEFAULT '0',
  `updated_at` int(11) DEFAULT '0',
  PRIMARY KEY (`id`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of primary_frequency
-- ----------------------------
BEGIN;
INSERT INTO `primary_frequency` (`id`, `over_enable`, `under_enable`, `rated_frequency`, `regulation_type`, `over_droop_rate_1`, `under_droop_rate_1`, `deadband_1`, `over_droop_rate_2`, `under_droop_rate_2`, `deadband_2`, `max_active_power_export_at_poi`, `max_active_power_import_at_poi`, `export_limit`, `import_limit`, `control_cycle`, `reset_time`, `updated_at`) VALUES (1, 1, 1, 50, 0, 5, 5, 0.2, 3, 5, 0.3, 12500, 12500, 0.3, 0.3, 1000, 2000, 0);
INSERT INTO `primary_frequency` (`id`, `over_enable`, `under_enable`, `rated_frequency`, `regulation_type`, `over_droop_rate_1`, `under_droop_rate_1`, `deadband_1`, `over_droop_rate_2`, `under_droop_rate_2`, `deadband_2`, `max_active_power_export_at_poi`, `max_active_power_import_at_poi`, `export_limit`, `import_limit`, `control_cycle`, `reset_time`, `updated_at`) VALUES (2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
COMMIT;

-- ----------------------------
-- Table structure for produce_wave_csv
-- ----------------------------
DROP TABLE IF EXISTS `produce_wave_csv`;
CREATE TABLE `produce_wave_csv` (
  `config_key` varchar(64) NOT NULL COMMENT '配置项键名',
  `config_value` varchar(256) NOT NULL COMMENT '配置项值',
  `config_desc` varchar(256) DEFAULT NULL COMMENT '配置项说明',
  `update_time` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '最后更新时间',
  PRIMARY KEY (`config_key`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of produce_wave_csv
-- ----------------------------
BEGIN;
INSERT INTO `produce_wave_csv` (`config_key`, `config_value`, `config_desc`, `update_time`) VALUES ('produce_wavecsv_enable', '0', '生成录波文件功能使能位：0-关闭，1-开启', '2026-03-30 16:55:12');
COMMIT;

-- ----------------------------
-- Table structure for reactive
-- ----------------------------
DROP TABLE IF EXISTS `reactive`;
CREATE TABLE `reactive` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `reactive_power` double DEFAULT '0',
  `slope_switch` tinyint(1) DEFAULT '0',
  `slope_control_cycle` int(11) DEFAULT '0',
  `up_slope` double DEFAULT '0',
  `down_slope` double DEFAULT '0',
  `updated_at` int(11) DEFAULT '0',
  PRIMARY KEY (`id`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of reactive
-- ----------------------------
BEGIN;
INSERT INTO `reactive` (`id`, `reactive_power`, `slope_switch`, `slope_control_cycle`, `up_slope`, `down_slope`, `updated_at`) VALUES (1, 0, 1, 6000, 100, 100, 1782111523);
INSERT INTO `reactive` (`id`, `reactive_power`, `slope_switch`, `slope_control_cycle`, `up_slope`, `down_slope`, `updated_at`) VALUES (2, 0, 0, 0, 0, 0, 0);
COMMIT;

-- ----------------------------
-- Table structure for station
-- ----------------------------
DROP TABLE IF EXISTS `station`;
CREATE TABLE `station` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `rated_active_power` double DEFAULT '0',
  `rated_apparent_power` double DEFAULT '0',
  `min_soc` double DEFAULT '0',
  `max_soc` double DEFAULT '0',
  `soc_balance_switch` tinyint(1) DEFAULT '0',
  `mode_switch` tinyint(1) DEFAULT '0',
  `priority_switch` tinyint(1) DEFAULT '0',
  `dispatch_lost` tinyint(1) DEFAULT '0',
  `updated_at` int(11) DEFAULT '0',
  PRIMARY KEY (`id`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of station
-- ----------------------------
BEGIN;
INSERT INTO `station` (`id`, `rated_active_power`, `rated_apparent_power`, `min_soc`, `max_soc`, `soc_balance_switch`, `mode_switch`, `priority_switch`, `dispatch_lost`, `updated_at`) VALUES (1, 12500, 12500, 5, 95, 0, 0, 0, 0, 0);
INSERT INTO `station` (`id`, `rated_active_power`, `rated_apparent_power`, `min_soc`, `max_soc`, `soc_balance_switch`, `mode_switch`, `priority_switch`, `dispatch_lost`, `updated_at`) VALUES (2, 100, 100, 10, 90, 0, 0, 0, 0, 1772500654);
COMMIT;

-- ----------------------------
-- Table structure for strategy_switch
-- ----------------------------
DROP TABLE IF EXISTS `strategy_switch`;
CREATE TABLE `strategy_switch` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `active_enable` tinyint(1) DEFAULT '0',
  `active_code` int(11) DEFAULT '0',
  `reactive_enable` tinyint(1) DEFAULT '0',
  `reactive_code` int(11) DEFAULT '0',
  `primary_frequency_enable` tinyint(1) DEFAULT '0',
  `system_switch` tinyint(1) DEFAULT '0',
  `updated_at` int(11) DEFAULT '0',
  `voltage_droop_enable` tinyint(1) DEFAULT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of strategy_switch
-- ----------------------------
BEGIN;
INSERT INTO `strategy_switch` (`id`, `active_enable`, `active_code`, `reactive_enable`, `reactive_code`, `primary_frequency_enable`, `system_switch`, `updated_at`, `voltage_droop_enable`) VALUES (1, 1, 1, 1, 1, 1, 1, 0, 1);
INSERT INTO `strategy_switch` (`id`, `active_enable`, `active_code`, `reactive_enable`, `reactive_code`, `primary_frequency_enable`, `system_switch`, `updated_at`, `voltage_droop_enable`) VALUES (2, 0, 0, 0, 0, 0, 0, 0, NULL);
COMMIT;

-- ----------------------------
-- Table structure for sys_translation
-- ----------------------------
DROP TABLE IF EXISTS `sys_translation`;
CREATE TABLE `sys_translation` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `key_value` int(11) NOT NULL COMMENT '关键字枚举',
  `value` text NOT NULL COMMENT '中文，源文字',
  `value_en_us` text COMMENT '英文',
  `value_ja_jp` text COMMENT '日文',
  `value_de_de` text COMMENT '德文',
  PRIMARY KEY (`id`),
  UNIQUE KEY `uk_key_value` (`key_value`)
) ENGINE=InnoDB AUTO_INCREMENT=81 DEFAULT CHARSET=utf8mb4 COMMENT='系统词条/操作日志模板';

-- ----------------------------
-- Records of sys_translation
-- ----------------------------
BEGIN;
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (1, 201, 'PCS开机，设定值{0}，设备{1}', 'PCS start-up, setpoint {0}, device(s) {1}', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (2, 202, 'PCS关机，设定值{0}，设备{1}', 'PCS shutdown, setpoint {0}, device(s) {1}', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (3, 203, 'PCS高压断路器合闸，设定值{0}，设备{1}', 'PCS HV breaker closed, setpoint {0}, device(s) {1}', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (4, 204, 'PCS高压断路器分闸，设定值{0}，设备{1}', 'PCS HV breaker opened, setpoint {0}, device(s) {1}', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (5, 205, 'PCS有功设置{0}kW，设备{1}', 'PCS active power set to {0} kW, device(s) {1}', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (6, 206, 'PCS无功设置{0}kVar，设备{1}', 'PCS reactive power set to {0} kVar, device(s) {1}', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (7, 207, 'PCS投退{0}，设备{1}', 'PCS service state {0}, device(s) {1}', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (8, 208, '整站PCS停机，值{0}，控制点{1}，来源{2}', 'Station-wide PCS shutdown, setpoint {0}, control point {1}, source {2}', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (9, 301, 'BMS开机，设定值{0}，设备{1}', 'BMS start-up, setpoint {0}, device(s) {1}', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (10, 302, 'BMS关机，设定值{0}，设备{1}', 'BMS shutdown, setpoint {0}, device(s) {1}', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (11, 401, '断路器合闸，设定值{0}，设备{1}', 'Breaker closed, setpoint {0}, device(s) {1}', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (12, 402, '断路器分闸，设定值{0}，设备{1}', 'Breaker opened, setpoint {0}, device(s) {1}', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (13, 501, '整站策略设置：有功{0}kW，视在{1}kVA，SOC上下限{2}-{3}%，本地/远程模式{4}，有功/无功优先{5}，调度中断控制方式{6}，SOC均衡{7}', 'Station strategy config: active power {0} kW, apparent power {1} kVA, SOC upper/lower limit {2}-{3}%, local/remote mode {4}, active/reactive priority {5}, dispatch interruption mode {6}, SOC balancing {7}', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (14, 502, '整站有功定值下发：{0} kW', 'Station active power setpoint issued: {0} kW', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (15, 503, '本地有功曲线修改，模式{0}，曲线名{1}', 'Local active curve updated, mode {0}, curve name {1}', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (16, 504, '远程有功曲线修改，模式{0}，曲线名{1}', 'Remote active curve updated, mode {0}, curve name {1}', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (17, 505, '整站无功定值下发：{0} kVar', 'Station reactive power setpoint issued: {0} kVar', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (18, 506, '本地无功曲线修改，模式{0}，曲线名{1}', 'Local reactive curve updated, mode {0}, curve name {1}', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (19, 507, '远程无功曲线修改，模式{0}，曲线名{1}', 'Remote reactive curve updated, mode {0}, curve name {1}', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (20, 508, '整站功率因数定值下发：{0}', 'Station power factor setpoint issued: {0}', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (21, 509, '一次调频：过频使能{0},欠频使能{1},额定频率{2},调频类型3/5{3},三段过频调差率{4},三段欠频调差率{5},三段调频死区{6},五段过频调差率{7},五段欠频调差率{8},五段调频死区{9},并网点最大输出有功{10}，并网点最大吸收有功{11}，输出限幅系数{12}%，吸收限幅系数{13}%，调节周期{14}ms，滞环复归时间{15}ms', 'Primary frequency regulation: over-freq enable {0}, under-freq enable {1}, rated frequency {2} Hz, regulation type 3/5-segment {3}, 3-seg over-freq droop {4}, 3-seg under-freq droop {5}, 3-seg freq deadband {6} Hz, 5-seg over-freq droop {7}, 5-seg under-freq droop {8}, 5-seg freq deadband {9} Hz, max active export at PCC {10} kW, max active import at PCC {11} kW, export limit factor {12}%, import limit factor {13}%, regulation period {14} ms, hysteresis recovery time {15} ms', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (22, 510, '电压下垂控制参数下发：下垂类型独立/截断{0}，曲线率类型3/5{1}，额定电压{2},三段电压调差系数{3}，3段调压死区{4}，五段电压调差系数{5}，五段调压死区{6}，并网点最大输出无功{7}，并网点最大吸收无功{8}，输出无功限幅系数{9}，吸收无功限幅系数{10}，调节周期{11}ms，滞环复归时间{12}ms', 'Voltage droop control issued: droop mode independent/truncated {0}, curve type 3/5-segment {1}, rated voltage {2} V, 3-seg voltage droop {3}, 3-seg voltage deadband {4} V, 5-seg voltage droop {5}, 5-seg voltage deadband {6} V, max reactive export at PCC {7} kVar, max reactive import at PCC {8} kVar, export limit factor {9}%, import limit factor {10}%, regulation period {11} ms, hysteresis recovery time {12} ms', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (23, 511, '电压固定值控制参数下发：电压上限{0}，电压下限{1}，上死区{2}，下死区{3}，调差系数{4}，调压定值{5}', 'Constant voltage control issued: voltage upper limit {0} V, voltage lower limit {1} V, upper deadband {2} V, lower deadband {3} V, droop coefficient {4}, voltage setpoint {5} V', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (24, 512, '斜率控制：有功控制:{0}/调节周期:{1}ms/上升斜率:{2}%/min/下降斜率:{3}%/min;无功控制:{4}/调节周期:{5}ms/上升斜率:{6}%/min/下降斜率:{7}%/min;电压控制:{8}/调节周期:{9}ms/上升斜率:{10}%/min/下降斜率:{11}%/min', 'Slope control: active {0}, period {1} ms, ramp up {2} %/min, ramp down {3} %/min; reactive {4}, period {5} ms, ramp up {6} %/min, ramp down {7} %/min; voltage {8}, period {9} ms, ramp up {10} %/min, ramp down {11} %/min', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (25, 513, '滤波控制：滤波类型{0}，使能开关{1}，滤波系数{2}', 'Filter control: filter type {0}, enable switch {1}, filter coefficient {2}', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (26, 514, 'PI控制：控制对象{0}，调节周期{1}ms，比例系数Kp{2}，积分系数Ki{3}，控制死区{4}，输出上限{5}，输出下限{6}，抗饱和使能{7}', 'PI control: control object {0}, regulation period {1} ms, proportional gain Kp {2}, integral gain Ki {3}, control deadband {4}, output upper limit {5}, output lower limit {6}, anti-windup enable {7}', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (27, 515, '惯量支撑：使能{0}，额定频率{1}Hz，频率幅值死区{2}Hz，频率变化率死区{3}Hz/s，虚拟惯量常数{4}s，最大输出有功{5}kW，最大吸收有功{6}kW，输出限幅系数{7}%，吸收限幅系数{8}%，频率上限{9}Hz，频率下限{10}Hz，闭锁使能{11}，调节周期{12}ms，滞环复归时间{13}ms', 'Virtual inertia support: enable {0}, rated frequency {1} Hz, freq amplitude deadband {2} Hz, ROCOF deadband {3} Hz/s, virtual inertia constant {4} s, max active export {5} kW, max active import {6} kW, export limit factor {7}%, import limit factor {8}%, freq upper limit {9} Hz, freq lower limit {10} Hz, blocking enable {11}, regulation period {12} ms, hysteresis recovery time {13} ms', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (28, 516, '黑启动策略参数修改', 'Black start strategy parameters updated', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (29, 517, '策略切换：有功{0}，模式{1}，无功{2}，模式{3}，调频{4}，下垂{5}，总开关{6}', 'Strategy switch: active power {0}/{1}, reactive power {2}/{3}, frequency regulation {4}, voltage droop {5}, master switch {6}', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (30, 701, '用户登录：{0}', 'User login: {0}', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (31, 702, '用户登出：{0}', 'User logout: {0}', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (32, 703, '新增用户：{0}，权限{1}', 'User created: {0}, permission level {1}', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (33, 704, '删除用户：{0}', 'User deleted: {0}', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (34, 705, '修改用户：{0}，字段{1}', 'User updated: {0}, modified fields {1}', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (35, 706, '修改密码：{0}', 'Password changed: {0}', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (36, 707, '重置密码：{0}', 'Password reset: {0}', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (37, 801, 'OTA升级包导入', 'OTA upgrade package imported', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (38, 802, 'OTA升级启动', 'OTA upgrade initiated', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (39, 803, '系统校时：{0}', 'System time synchronized: {0}', NULL, NULL);
INSERT INTO `sys_translation` (`id`, `key_value`, `value`, `value_en_us`, `value_ja_jp`, `value_de_de`) VALUES (40, 804, '录波控制，模式{0}，状态{1}，标题{2}，间隔{3}，时长{4}', 'Waveform recording control: mode {0}, status {1}, title {2}, sampling interval {3} ms, duration {4} s', NULL, NULL);
COMMIT;

-- ----------------------------
-- Table structure for user
-- ----------------------------
DROP TABLE IF EXISTS `user`;
CREATE TABLE `user` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `username` varchar(50) NOT NULL DEFAULT '',
  `password` varchar(255) NOT NULL DEFAULT '',
  `contact_info` varchar(100) DEFAULT '',
  `remark` varchar(255) DEFAULT '',
  `updated_at` int(11) DEFAULT '0',
  `permission` tinyint(1) DEFAULT NULL,
  PRIMARY KEY (`id`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of user
-- ----------------------------
BEGIN;
INSERT INTO `user` (`id`, `username`, `password`, `contact_info`, `remark`, `updated_at`, `permission`) VALUES (1, 'admin', 'd8578edf8458ce06fbc5bb76a58c5ca4', '13486882148', '1', 1780036058, 1);
INSERT INTO `user` (`id`, `username`, `password`, `contact_info`, `remark`, `updated_at`, `permission`) VALUES (2, 'strategy', '96f0f08c0188ba04898ce8cc465c19c4', '', '', 1778226969, 1);
INSERT INTO `user` (`id`, `username`, `password`, `contact_info`, `remark`, `updated_at`, `permission`) VALUES (3, 'ITbieluangao', '2878fefc292273f2f16beecc2067eed8', '13955538695', '', 1780036044, 1);
COMMIT;

-- ----------------------------
-- Table structure for voltage_droop
-- ----------------------------
DROP TABLE IF EXISTS `voltage_droop`;
CREATE TABLE `voltage_droop` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `curve_type` tinyint(1) DEFAULT '0',
  `regulation_type` tinyint(1) DEFAULT '0',
  `rated_voltage` double DEFAULT '0',
  `droop_coefficient_1` double DEFAULT '0',
  `deadband_1` double DEFAULT '0',
  `droop_coefficient_2` double DEFAULT '0',
  `deadband_2` double DEFAULT '0',
  `max_reactive_power_export_at_poi` double DEFAULT '0',
  `max_reactive_power_import_at_poi` double DEFAULT '0',
  `export_limit` double DEFAULT '0',
  `import_limit` double DEFAULT '0',
  `control_cycle` int(11) DEFAULT '0',
  `reset_time` int(11) DEFAULT '0',
  `updated_at` int(11) DEFAULT '0',
  PRIMARY KEY (`id`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of voltage_droop
-- ----------------------------
BEGIN;
INSERT INTO `voltage_droop` (`id`, `curve_type`, `regulation_type`, `rated_voltage`, `droop_coefficient_1`, `deadband_1`, `droop_coefficient_2`, `deadband_2`, `max_reactive_power_export_at_poi`, `max_reactive_power_import_at_poi`, `export_limit`, `import_limit`, `control_cycle`, `reset_time`, `updated_at`) VALUES (1, 1, 1, 220000, 5000, 0.05, 3000, 0.3, 12500, 12500, 1, 1, 10000, 5000, 0);
INSERT INTO `voltage_droop` (`id`, `curve_type`, `regulation_type`, `rated_voltage`, `droop_coefficient_1`, `deadband_1`, `droop_coefficient_2`, `deadband_2`, `max_reactive_power_export_at_poi`, `max_reactive_power_import_at_poi`, `export_limit`, `import_limit`, `control_cycle`, `reset_time`, `updated_at`) VALUES (2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
COMMIT;

-- ----------------------------
-- Table structure for voltage_fixed
-- ----------------------------
DROP TABLE IF EXISTS `voltage_fixed`;
CREATE TABLE `voltage_fixed` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `max_voltage` double DEFAULT '0',
  `min_voltage` double DEFAULT '0',
  `high_deadband` double DEFAULT '0',
  `low_deadband` double DEFAULT '0',
  `droop_coefficient` double DEFAULT '0',
  `voltage` double DEFAULT '0',
  `slope_switch` tinyint(1) DEFAULT '0',
  `slope_control_cycle` int(11) DEFAULT '0',
  `up_slope` double DEFAULT '0',
  `down_slope` double DEFAULT '0',
  `updated_at` int(11) DEFAULT '0',
  PRIMARY KEY (`id`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;

-- ----------------------------
-- Records of voltage_fixed
-- ----------------------------
BEGIN;
INSERT INTO `voltage_fixed` (`id`, `max_voltage`, `min_voltage`, `high_deadband`, `low_deadband`, `droop_coefficient`, `voltage`, `slope_switch`, `slope_control_cycle`, `up_slope`, `down_slope`, `updated_at`) VALUES (1, 100.2, 99.8, 100.1, 99.9, 5000, 0, 1, 10000, 1, 1, 1782111523);
INSERT INTO `voltage_fixed` (`id`, `max_voltage`, `min_voltage`, `high_deadband`, `low_deadband`, `droop_coefficient`, `voltage`, `slope_switch`, `slope_control_cycle`, `up_slope`, `down_slope`, `updated_at`) VALUES (2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
COMMIT;

SET FOREIGN_KEY_CHECKS = 1;
