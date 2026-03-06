cmake_minimum_required(VERSION 3.16)
project(smlcore_native LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

add_library(smlcore_native SHARED
    src/smlcore_native.cpp
)

target_include_directories(smlcore_native PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}/include
)

include(CTest)
if(BUILD_TESTING)
    add_executable(smlcore_native_spec_tests
        tests/spec_tests.cpp
    )
    target_link_libraries(smlcore_native_spec_tests PRIVATE smlcore_native)
    set(SML_SPEC_CASES
        single_node
        nested_nodes
        ast_json_roundtrip
        unterminated_string
    )
    foreach(CASE_NAME IN LISTS SML_SPEC_CASES)
        add_test(NAME smlcore_native_spec_${CASE_NAME} COMMAND smlcore_native_spec_tests ${CASE_NAME})
    endforeach()

    add_executable(smlcore_native_parser_tests
        tests/parser_tests.cpp
    )
    target_link_libraries(smlcore_native_parser_tests PRIVATE smlcore_native)
    set(SML_PARSER_CASES
        sml_parser_nested_nodes_parse_structure
        sml_parser_identifier_property_parses
        sml_parser_unknown_node_kept_in_ast
        sml_parser_float_values_parse_successfully
        sml_parser_invalid_float_format_rejected
        sml_parser_unregistered_identifier_property_is_identifier
        sml_parser_tuple_values_parse_comma_and_pipe
        sml_parser_multiline_string_preserves_linebreaks
        sml_parser_resource_ref_parses_as_identifier_token
        sml_parser_comments_do_not_break_parse
    )
    foreach(CASE_NAME IN LISTS SML_PARSER_CASES)
        add_test(NAME ${CASE_NAME} COMMAND smlcore_native_parser_tests ${CASE_NAME})
    endforeach()
endif()
