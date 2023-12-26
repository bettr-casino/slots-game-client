UNITY_APP := "${UNITY_HOME}/2022.3.16f1/Unity.app/Contents/MacOS/Unity"
UNITY_PROJECT_PATH :="Unity"

BUILD_OUTPUT_PATH:= ${BETTR_CASINO_IOS_BUILDS_HOME}
BUILD_LOGS_PATH:= ${BETTR_CASINO_IOS_LOGS_HOME}
BUILD_METHOD := "CommandLine.BuildIOS"

.PHONY: build_ios

build_ios:
	@echo "Building iOS project..."
	@$(UNITY_APP) -quit -batchmode -logFile $(BUILD_LOGS_PATH)/logfile.log -projectPath $(UNITY_PROJECT_PATH) -executeMethod $(BUILD_METHOD) -buildOutput $(BUILD_OUTPUT_PATH)
	@echo "Build completed."


