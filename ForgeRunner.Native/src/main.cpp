#include <chrono>
#include <ctime>
#include <iostream>
#include <string>
#include <thread>
#include <vector>

namespace {

std::string current_timestamp_utc() {
    using clock = std::chrono::system_clock;
    const auto now = clock::now();
    const std::time_t tt = clock::to_time_t(now);
    std::tm tm{};
#if defined(_WIN32)
    gmtime_s(&tm, &tt);
#else
    gmtime_r(&tt, &tm);
#endif

    char buffer[32]{};
    std::strftime(buffer, sizeof(buffer), "%Y-%m-%dT%H:%M:%SZ", &tm);
    return std::string(buffer);
}

void print_usage() {
    std::cout << "ForgeRunner.Native\n"
              << "Usage:\n"
              << "  forge-runner-native [--url <value>] [--verbose] [--help]\n";
}

}  // namespace

int main(int argc, char** argv) {
    std::vector<std::string> args;
    args.reserve(argc > 0 ? static_cast<std::size_t>(argc) : 0U);
    for (int i = 0; i < argc; ++i) {
        args.emplace_back(argv[i] != nullptr ? argv[i] : "");
    }

    bool verbose = false;
    std::string url;

    for (int i = 1; i < argc; ++i) {
        const std::string arg = args[static_cast<std::size_t>(i)];
        if (arg == "--help" || arg == "-h") {
            print_usage();
            return 0;
        }
        if (arg == "--verbose") {
            verbose = true;
            continue;
        }
        if (arg == "--url" && i + 1 < argc) {
            url = args[static_cast<std::size_t>(++i)];
            continue;
        }
        if (arg.rfind("--url=", 0) == 0) {
            url = arg.substr(6);
            continue;
        }
    }

    std::cout << "[ForgeRunner.Native] bootstrap started at " << current_timestamp_utc() << "\n";
    std::cout << "[ForgeRunner.Native] executable main is active.\n";
    if (!url.empty()) {
        std::cout << "[ForgeRunner.Native] url=" << url << "\n";
    } else {
        std::cout << "[ForgeRunner.Native] url=<none> (last-url restore behavior pending native runtime integration)\n";
    }
    if (verbose) {
        std::cout << "[ForgeRunner.Native] verbose=true\n";
    }

    // Keep process alive briefly so startup is visible in shells that auto-close child output.
    std::this_thread::sleep_for(std::chrono::milliseconds(50));
    return 0;
}
