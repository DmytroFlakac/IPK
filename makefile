PROJECT_NAME=ipk24chat-client
TARGET_FRAMEWORK=net8.0
RUNTIME=linux-x64
# Since the output should be in the project root, we adjust PUBLISH_DIR accordingly
PUBLISH_DIR=.
obj=obj
bin=bin
OUTPUT_NAME=ipk24chat-client

.PHONY: build clean

# The default target
default: build

# Build target
build:
	dotnet publish $(PROJECT_NAME).csproj -c Release -r $(RUNTIME) --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true -o $(PUBLISH_DIR)
	# Ensure the output is named as specified. Adjusting this line to ensure correct renaming.
	mv $(PUBLISH_DIR)/$(PROJECT_NAME) $(PUBLISH_DIR)/$(OUTPUT_NAME) || true

# Clean target
clean:
	rm -f $(PUBLISH_DIR)/$(OUTPUT_NAME)
	rm -rf $(PUBLISH_DIR)/$(OUTPUT_NAME).pdb
 
destroy: 
	rm -f $(PUBLISH_DIR)/$(OUTPUT_NAME)
	rm -rf $(PUBLISH_DIR)/$(OUTPUT_NAME).pdb
	rm -rf $(PUBLISH_DIR)/$(obj)
	rm -rf $(PUBLISH_DIR)/$(bin)