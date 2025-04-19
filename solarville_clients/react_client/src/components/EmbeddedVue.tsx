import {applyVueInReact, applyPureVueInReact} from 'veaury'
// import {applyVueInReact, applyPureVueInReact} from 'veaury/types/veaury.d.ts'

// This is a Vue component
import BasicVueComponent from './Basic.vue'
import {useState} from 'react'
// Use HOC 'applyVueInReact'
const BasicWithNormal = applyVueInReact(BasicVueComponent)
// Use HOC 'applyPureVueInReact'
const BasicWithPure = applyPureVueInReact(BasicVueComponent)
export default function () {
  const [foo] = useState('Hello!')
  return <>
    <BasicWithNormal foo={foo}>
      <div>
        the default slot
      </div>
    </BasicWithNormal>
    <BasicWithPure foo={foo}>
      <div>
        the default slot
      </div>
    </BasicWithPure>
  </>
}