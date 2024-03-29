PROJECT_NAME=IPK
TARGET_FRAMEWORK=net8.0
RUNTIME=linux-x64
PUBLISH_DIR=./root
OUTPUT_NAME=ipk24chat-client

.PHONY: build clean

default: build

build:
	dotnet publish $(PROJECT_NAME).csproj -c Release -r $(RUNTIME) --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true -o $(PUBLISH_DIR)
	mv $(PUBLISH_DIR)/$(PROJECT_NAME) $(OUTPUT_NAME) || true

clean:
	rm -f $(OUTPUT_NAME)
	rm -rf $(PUBLISH_DIR)