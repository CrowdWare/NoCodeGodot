Manifest {
    version: 1
    baseUrl: "https://example.com/content/"
    entryPoint: "ui/main.sml"

    Asset {
        id: "ui-main"
        path: "ui/main.sml"
        hash: "sha256:5e1f5f7f3d0d7a4f111111111111111111111111111111111111111111111111"
        type: "ui"
        size: 1024
    }

    Asset {
        id: "paladin-model"
        path: "models/paladin.glb"
        hash: "sha256:8d56b8f66f8f6f72222222222222222222222222222222222222222222222222"
        type: "model"
        size: 2456789
    }

    Asset {
        id: "logo"
        path: "textures/logo.png"
        url: "https://cdn.example.com/nocode/logo.png"
        hash: "sha256:6ce38f37adfca1a3333333333333333333333333333333333333333333333333"
        type: "texture"
        size: 45123
    }
}
