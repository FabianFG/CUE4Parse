#include <cstring>
#include <exception>
#include <filesystem>
#include <fstream>

#include "Framework.h"
#include "UsmapGen/UsmapGen.h"

namespace {
    class MemorySource final : public UsmapGen::Source {
    public:
        explicit MemorySource(std::span<const std::byte> data) : bytes(data) {}

        std::uint64_t getSize() const noexcept override { return bytes.size(); }
        std::span<const std::byte> getBytes() const noexcept override { return bytes; }

        std::expected<void, UsmapGen::Error> read(std::uint64_t offset, std::span<std::byte> buffer) const noexcept override {
            if (offset > bytes.size() || buffer.size() > bytes.size() - offset)
                return std::unexpected(UsmapGen::Error{ "The executable cannot be read." });

            std::memcpy(buffer.data(), bytes.data() + offset, buffer.size());
            return {};
        }

    private:
        std::span<const std::byte> bytes;
    };

    using ReportDiagnostic = void(__cdecl*)(const char* message);
}

DLLEXPORT bool __cdecl nGenerateUsmapFromMemory(const std::byte* bytes, std::size_t size, const wchar_t* output, std::uint8_t compression, ReportDiagnostic report) noexcept {
    try {
        const MemorySource source(std::span(bytes, size));
        const auto mapping = UsmapGen::generateUsmap(source, { .compression = static_cast<UsmapGen::Compression>(compression) });
        if (!mapping) {
            report(mapping.error().message.c_str());
            return false;
        }

        std::ofstream file(std::filesystem::path(output), std::ios::binary);
        file.write(reinterpret_cast<const char*>(mapping->bytes.data()), static_cast<std::streamsize>(mapping->bytes.size()));
        file.close();
        if (!file) {
            report("The generated mapping could not be written.");
            return false;
        }

        for (const auto& warning : mapping->warnings)
            report(warning.c_str());

        return true;
    }
    catch (const std::exception& ex) {
        report(ex.what());
    }
    catch (...) {
        report("An unexpected native error occurred.");
    }
    return false;
}
