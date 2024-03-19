# Compiler
DOTNET=dotnet

# Output path
OUTPUT_DIR=./roo
OUTPUT=$(OUTPUT_DIR)/ipk24chat-client.exe

# Default target
all: $(OUTPUT)

# Rule for creating the executable
$(OUTPUT):
	$(DOTNET) build -c Release -o $(OUTPUT_DIR)

# Clean target
clean:
	$(DOTNET) clean
	rm -rf $(OUTPUT_DIR)
