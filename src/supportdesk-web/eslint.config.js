import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import tseslint from 'typescript-eslint'
import { defineConfig, globalIgnores } from 'eslint/config'

export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      js.configs.recommended,
      tseslint.configs.recommended,
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      globals: globals.browser,
    },
    rules: {
      // Data is loaded from the API inside effects on mount and on filter
      // changes. That intentionally updates state after an external request,
      // which the new synchronous-setState heuristic cannot distinguish from
      // cascading render bugs.
      'react-hooks/set-state-in-effect': 'off',
      // Context modules intentionally export both a provider component and a
      // hook; fast refresh falls back to a full reload for those files.
      'react-refresh/only-export-components': 'off',
    },
  },
])
