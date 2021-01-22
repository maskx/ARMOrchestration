-- ============================================
-- {0} Schema
-- {1} Hub
-- ============================================

DROP PROCEDURE IF EXISTS [{0}].[{1}_PrepareRetry]
DROP PROCEDURE IF EXISTS [{0}].[{1}_CreateDeploymentOperation]
DROP TABLE IF EXISTS [{0}].[{1}_WaitDependsOn]
DROP TABLE IF EXISTS [{0}].[{1}_DeploymentOperations]
DROP TABLE IF EXISTS [{0}].[{1}_DeploymentOperationHistory]